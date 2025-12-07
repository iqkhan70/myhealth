using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Helpers
{
    /// <summary>
    /// Custom IQueryable wrapper that decrypts User PII data when enumerated
    /// This allows OData to apply filters/sorting/pagination on encrypted data,
    /// then decrypts the results before serialization
    /// </summary>
    public class DecryptingQueryable<T> : IQueryable<T> where T : class
    {
        private readonly IQueryable<T> _source;
        private readonly IPiiEncryptionService _encryptionService;

        public DecryptingQueryable(IQueryable<T> source, IPiiEncryptionService encryptionService)
        {
            _source = source;
            _encryptionService = encryptionService;
        }

        public Type ElementType => _source.ElementType;
        public Expression Expression => _source.Expression;
        public IQueryProvider Provider => _source.Provider;

        public IEnumerator<T> GetEnumerator()
        {
            Console.WriteLine($"[DecryptingQueryable] GetEnumerator called for type {typeof(T).Name}");
            var items = _source.ToList();
            Console.WriteLine($"[DecryptingQueryable] Materialized {items.Count} items from source");
            
            // Decrypt if T is User
            if (typeof(T) == typeof(User))
            {
                var users = items.Cast<User>().ToList();
                Console.WriteLine($"[DecryptingQueryable] Decrypting {users.Count} User entities");
                UserEncryptionHelper.DecryptUserData(users, _encryptionService);
                Console.WriteLine($"[DecryptingQueryable] Decryption complete, returning enumerator");
                return users.Cast<T>().GetEnumerator();
            }
            
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

