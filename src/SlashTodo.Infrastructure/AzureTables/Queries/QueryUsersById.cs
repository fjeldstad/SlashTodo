using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.AzureTables.Queries.Entities;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Infrastructure.AzureTables.Queries
{
    public class QueryUsersById : 
        QueryBase,
        QueryUsers.IById
    {
        public const string DefaultTableName = "queryUsersById";

        public QueryUsersById(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryUsersById(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount, tableName)
        {
        }

        public async Task<UserDto> ById(string id)
        {
            var table = await StorageAccount.GetTableAsync(TableName).ConfigureAwait(false);
            var entity = await table.RetrieveAsync<UserDtoTableEntity>(id, id).ConfigureAwait(false);
            return entity != null ? entity.GetDto() : null;
        }

        protected override IEnumerable<ISubscriptionToken> RegisterSubscriptionsCore(ISubscriptionRegistry registry)
        {
            yield return registry.RegisterSubscription<UserCreated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                table.Insert(new UserDtoTableEntity(new UserDto
                {
                    Id = @event.Id,
                    TeamId = @event.TeamId,
                    CreatedAt = @event.Timestamp
                }));
            });
            yield return registry.RegisterSubscription<UserNameUpdated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<UserDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.Name = @event.Name;
                table.Update(entity);
            });
            yield return registry.RegisterSubscription<UserSlackApiAccessTokenUpdated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<UserDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.SlackApiAccessToken = @event.SlackApiAccessToken;
                table.Update(entity);
            });
        }
    }
}
