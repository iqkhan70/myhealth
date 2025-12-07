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
                else if (objectResult.Value is System.Collections.IEnumerable enumerable)
                {
                    // Only process if the enumerable contains User objects
                    // OData responses might contain dictionaries or other types
                    var firstItem = enumerable.Cast<object>().FirstOrDefault();
                    if (firstItem is User)
                    {
                        var users = enumerable.Cast<User>().ToList();
                        if (users.Any())
                        {
                            UserEncryptionHelper.DecryptUserData(users, _encryptionService);
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
                    var users = queryable.ToList();
                    if (users.Any())
                    {
                        UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                        // Replace the queryable with the decrypted list
                        objectResult.Value = users.AsQueryable();
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

