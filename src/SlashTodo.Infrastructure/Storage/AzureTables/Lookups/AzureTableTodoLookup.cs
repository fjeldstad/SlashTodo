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
    public class AzureTableTodoLookup :
        TableStorageBase<LookupAggregateIdByStringTableEntity>,
        ITodoLookup,
        ISubscriber
    {
        public const string DefaultTableName = "todoIdBySlackConversationIdAndShortCode";
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public AzureTableTodoLookup(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public AzureTableTodoLookup(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _tableName = tableName;
        }

        public async Task<Guid?> BySlackConversationIdAndShortCode(string slackConversationId, string shortCode)
        {
            var entity = await Retrieve(_tableName, slackConversationId, shortCode).ConfigureAwait(false);
            return entity != null ? entity.AggregateId : (Guid?)null;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(
                registry.RegisterSubscription<TodoAdded>(@event =>
                    Insert(new LookupAggregateIdByStringTableEntity(@event.SlackConversationId, @event.ShortCode, @event.Id), _tableName).Wait()));
            _subscriptionTokens.Add(
                registry.RegisterSubscription<TodoRemoved>(@event =>
                    Delete(_tableName, @event.SlackConversationId, @event.ShortCode).Wait()));
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
