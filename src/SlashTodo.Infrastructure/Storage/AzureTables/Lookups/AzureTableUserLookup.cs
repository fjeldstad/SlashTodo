using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Lookups;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Lookups
{
    public class AzureTableUserLookup : 
        TableStorageBase<LookupAggregateIdByStringTableEntity>, 
        IUserLookup, 
        ISubscriber
    {
        public const string DefaultTableName = "userIdBySlackUserId";
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public AzureTableUserLookup(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public AzureTableUserLookup(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _tableName = tableName;
        }

        public async Task<Guid?> BySlackUserId(string slackUserId)
        {
            var entity = await Retrieve(_tableName, slackUserId, slackUserId).ConfigureAwait(false);
            return entity != null ? entity.AggregateId : (Guid?)null;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(
                registry.RegisterSubscription<UserCreated>(@event =>
                    Insert(new LookupAggregateIdByStringTableEntity(@event.SlackUserId, @event.Id), _tableName).Wait()));
        }

        public void Dispose()
        {
            foreach (var token in _subscriptionTokens)
            {
                token.Dispose();
            }
        }
    }
}
