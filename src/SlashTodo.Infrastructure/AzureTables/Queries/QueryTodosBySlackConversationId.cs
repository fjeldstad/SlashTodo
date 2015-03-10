using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.AzureTables.Queries.Entities;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Infrastructure.AzureTables.Queries
{
    public class QueryTodosBySlackConversationId :
        QueryTodos.IBySlackConversationId,
        ISubscriber
    {
        public const string DefaultTableName = "queryTodosBySlackConversationId";

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public QueryTodosBySlackConversationId(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryTodosBySlackConversationId(CloudStorageAccount storageAccount, string tableName)
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

        public Task<TodoDto[]> BySlackConversationId(string slackConversationId)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            // TODO
            throw new NotImplementedException();
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
