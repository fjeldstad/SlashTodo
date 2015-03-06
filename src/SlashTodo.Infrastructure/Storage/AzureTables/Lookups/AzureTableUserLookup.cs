using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Lookups;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Lookups
{
    public class AzureTableUserLookup : 
        TableStorageBase<LookupAggregateIdByStringTableEntity>, 
        IUserLookup, 
        ISubscriber<UserCreated>
    {
        public const string DefaultTableName = "userIdBySlackUserId";
        private readonly string _tableName;

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

        public async Task HandleEvent(UserCreated @event)
        {
            await Insert(new LookupAggregateIdByStringTableEntity(@event.SlackUserId, @event.Id), _tableName).ConfigureAwait(false);
        }
    }
}
