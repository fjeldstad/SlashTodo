using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.AzureTables.Queries.Entities;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Infrastructure.AzureTables.Queries
{
    public class QueryTodosBySlackConversationId :
        QueryBase,
        QueryTodos.IBySlackConversationId
    {
        public const string DefaultTableName = "queryTodosBySlackConversationId";

        public QueryTodosBySlackConversationId(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryTodosBySlackConversationId(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount, tableName)
        {
        }

        public async Task<TodoDto[]> BySlackConversationId(string slackConversationId)
        {
            if (string.IsNullOrWhiteSpace(slackConversationId))
            {
                throw new ArgumentNullException("slackConversationId");
            }
            var table = await StorageAccount.GetTableAsync(TableName).ConfigureAwait(false);
            var entities = await table.RetrievePartitionAsync<TodoDtoTableEntity>(slackConversationId).ConfigureAwait(false);
            return entities.Select(x => x.GetTodoDto()).ToArray();
        }

        protected override IEnumerable<ISubscriptionToken> RegisterSubscriptionsCore(ISubscriptionRegistry registry)
        {
            yield return registry.RegisterSubscription<TodoAdded>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                table.Insert(new TodoDtoTableEntity(new TodoDto
                {
                    Id = @event.Id,
                    SlackConversationId = @event.SlackConversationId,
                    ShortCode = @event.ShortCode,
                    TeamId = @event.TeamId,
                    Text = @event.Text,
                    CreatedAt = @event.Timestamp
                }, x => x.SlackConversationId, x => x.ShortCode));
            });
            yield return registry.RegisterSubscription<TodoRemoved>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                table.Delete(@event.SlackConversationId, @event.ShortCode);
            });
            yield return registry.RegisterSubscription<TodoTicked>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TodoDtoTableEntity>(@event.SlackConversationId, @event.ShortCode);
                if (entity != null)
                {
                    entity.CompletedAt = @event.Timestamp;
                    entity.ClaimedByUserId = null;
                    table.Update(entity);
                }
            });
            yield return registry.RegisterSubscription<TodoUnticked>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TodoDtoTableEntity>(@event.SlackConversationId, @event.ShortCode);
                if (entity != null)
                {
                    entity.CompletedAt = null;
                    table.Update(entity);
                }
            });
            yield return registry.RegisterSubscription<TodoClaimed>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TodoDtoTableEntity>(@event.SlackConversationId, @event.ShortCode);
                if (entity != null)
                {
                    entity.ClaimedByUserId = @event.UserId;
                    table.Update(entity);
                }
            });
            yield return registry.RegisterSubscription<TodoFreed>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TodoDtoTableEntity>(@event.SlackConversationId, @event.ShortCode);
                if (entity != null)
                {
                    entity.ClaimedByUserId = null;
                    table.Update(entity);
                }
            });
        }
    }
}
