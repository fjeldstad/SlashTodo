using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Infrastructure.AzureTables.Queries
{
    public abstract class QueryBase :
        ISubscriber
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        protected CloudStorageAccount StorageAccount { get { return _storageAccount; } }
        public string TableName { get { return _tableName; } }

        protected QueryBase(CloudStorageAccount storageAccount, string tableName)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _storageAccount = storageAccount;
            _tableName = tableName;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.AddRange(RegisterSubscriptionsCore(registry));
        }

        protected abstract IEnumerable<ISubscriptionToken> RegisterSubscriptionsCore(ISubscriptionRegistry registry);

        public void Dispose()
        {
            foreach (var token in _subscriptionTokens)
            {
                token.Dispose();
            }
        }
    }
}
