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
    public class AzureTableAccountLookup : 
        TableStorageBase<LookupAggregateIdByStringTableEntity>, 
        IAccountLookup, 
        ISubscriber<AccountCreated>
    {
        public const string DefaultTableName = "accountIdBySlackTeamId";
        private readonly string _tableName;

        public string TableName { get { return _tableName; } }

        public AzureTableAccountLookup(CloudStorageAccount storageAccount, string tableName = DefaultTableName)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _tableName = tableName;
        }

        public async Task<Guid?> BySlackTeamId(string slackTeamId)
        {
            var entity = await Retrieve(_tableName, slackTeamId, slackTeamId);
            return entity != null ? entity.AggregateId : (Guid?)null;
        }

        public Task HandleEvent(AccountCreated @event)
        {
            return Insert(new LookupAggregateIdByStringTableEntity(@event.SlackTeamId, @event.Id), _tableName);
        }
    }
}
