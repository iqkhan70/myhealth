using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Results;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;
using System.Linq;

namespace SM_MentalHealthApp.Server.Filters
{
    /// <summary>
    /// Result filter that decrypts User PII data in OData responses before serialization
    /// </summary>
    public class ODataUserDecryptionActionFilter : IResultFilter
    {
        private readonly IPiiEncryptionService _encryptionService;

        public ODataUserDecryptionActionFilter(IPiiEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            // Check if the request has DateOfBirth in orderBy - if so, the controller should have already handled it
            // Don't try to enumerate IQueryable that might have DateOfBirth operations
            var request = context.HttpContext.Request;
            var orderByParam = request.Query["$orderby"].ToString();
            var hasDateOfBirthInOrderBy = !string.IsNullOrEmpty(orderByParam) && 
                                         orderByParam.Contains("DateOfBirth", StringComparison.OrdinalIgnoreCase);
            
            // Decrypt before serialization
            if (context.Result is ObjectResult objectResult && objectResult.Value != null)
            {
                // Handle PageResult<User> (OData paginated results)
                if (objectResult.Value is PageResult<User> pageResult)
                {
                    var users = pageResult.Items?.Cast<User>().ToList();
                    if (users != null && users.Any())
                    {
                        UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                    }
                }
                // Handle IEnumerable<User> (collection results)
                // But be careful - OData might return other types, so check if it's actually User
                else if (objectResult.Value is System.Collections.IEnumerable enumerable && !(objectResult.Value is IQueryable<User>))
                {
                    // Only process if the enumerable contains User objects
                    // OData responses might contain dictionaries or other types
                    // Skip if DateOfBirth is in orderBy (controller should have handled it)
                    if (!hasDateOfBirthInOrderBy)
                    {
                        try
                        {
                            var firstItem = enumerable.Cast<object>().FirstOrDefault();
                            if (firstItem is User)
                            {
                                var users = enumerable.Cast<User>().ToList();
                                if (users.Any())
                                {
                                    UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                                }
                            }
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("DateOfBirth") || ex.Message.Contains("Translation") || ex.Message.Contains("unmapped"))
                        {
                            // Skip if translation error - controller should have handled DateOfBirth
                        }
                    }
                    // If it's not User objects, skip decryption (might be OData metadata or other types)
                }
                // Handle SingleResult<User>
                else if (objectResult.Value is SingleResult<User> singleResult)
                {
                    var user = singleResult.Queryable.FirstOrDefault();
                    if (user != null)
                    {
                        UserEncryptionHelper.DecryptUserData(user, _encryptionService);
                    }
                }
                // Handle IQueryable<User> - materialize and decrypt
                else if (objectResult.Value is IQueryable<User> queryable)
                {
                    // If DateOfBirth is in orderBy, the controller should have already handled it
                    // Skip enumeration to avoid EF Core trying to translate DateOfBirth
                    if (hasDateOfBirthInOrderBy)
                    {
                        // The controller should have already materialized and decrypted
                        // Just check if it's an in-memory enumerable and decrypt if needed
                        if (queryable is System.Collections.Generic.IEnumerable<User> inMemoryEnumerable)
                        {
                            var users = inMemoryEnumerable.ToList();
                            if (users.Any())
                            {
                                // Check if DateOfBirth is already decrypted
                                var needsDecryption = users.Any(u => u.DateOfBirth == DateTime.MinValue || 
                                                                   string.IsNullOrEmpty(u.MobilePhone));
                                if (needsDecryption)
                                {
                                    UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                                    objectResult.Value = users.AsQueryable();
                                }
                            }
                        }
                        // If it's not an in-memory enumerable, don't try to enumerate - let OData handle it
                        return;
                    }
                    
                    // Check if this is already a materialized list (from AsQueryable() on a List)
                    // If it's backed by EF Core, we need to be careful about DateOfBirth operations
                    try
                    {
                        // Try to check if this is an in-memory queryable (from AsQueryable on a List)
                        // by checking if it's actually an IEnumerable<User> that we can enumerate safely
                        if (queryable is System.Collections.Generic.IEnumerable<User> inMemoryEnumerable)
                        {
                            // This is likely already materialized - just decrypt if needed
                            var users = inMemoryEnumerable.ToList();
                            if (users.Any())
                            {
                                // Check if DateOfBirth is already decrypted (if it's not DateTime.MinValue, it's likely decrypted)
                                // If it's already decrypted, we don't need to decrypt again
                                var needsDecryption = users.Any(u => u.DateOfBirth == DateTime.MinValue || 
                                                                   string.IsNullOrEmpty(u.MobilePhone));
                                
                                if (needsDecryption)
                                {
                                    UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                                }
                                // Replace the queryable with the decrypted list
                                objectResult.Value = users.AsQueryable();
                            }
                        }
                        else
                        {
                            // This is an EF Core queryable - materialize it
                            var users = queryable.ToList();
                            if (users.Any())
                            {
                                UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                                // Replace the queryable with the decrypted list
                                objectResult.Value = users.AsQueryable();
                            }
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("DateOfBirth") || ex.Message.Contains("Translation") || ex.Message.Contains("unmapped"))
                    {
                        // If we get a translation error, it means the query still has DateOfBirth operations
                        // This means the controller didn't handle DateOfBirth operations properly
                        // OR the queryable is from an EF Core query that wasn't materialized
                        // In this case, we should NOT try to enumerate it - let OData handle it
                        // The controller should have materialized DateOfBirth operations before returning
                        // Don't try to enumerate - just let OData serialize whatever is in the queryable
                        // The controller should have already materialized and decrypted
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            // No action needed after execution
        }
    }
}

