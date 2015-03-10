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
        QueryUsers.IById,
        ISubscriber
    {
        public const string DefaultTableName = "queryUsersById";

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public QueryUsersById(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryUsersById(CloudStorageAccount storageAccount, string tableName)
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

        public async Task<UserDto> ById(string id)
        {
            var table = await _storageAccount.GetTableAsync(_tableName).ConfigureAwait(false);
            var entity = await table.RetrieveAsync<UserDtoTableEntity>(id, id).ConfigureAwait(false);
            return entity != null ? entity.GetDto() : null;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(registry.RegisterSubscription<UserCreated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                table.Insert(new UserDtoTableEntity(new UserDto
                {
                    Id = @event.Id,
                    TeamId = @event.TeamId,
                    CreatedAt = @event.Timestamp
                }));
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<UserNameUpdated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<UserDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.Name = @event.Name;
                table.Update(entity);
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<UserSlackApiAccessTokenUpdated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<UserDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.SlackApiAccessToken = @event.SlackApiAccessToken;
                table.Update(entity);
            }));
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
