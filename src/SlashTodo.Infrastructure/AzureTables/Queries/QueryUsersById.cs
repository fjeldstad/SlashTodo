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
        TableStorageBase<UserDtoTableEntity>, 
        QueryUsers.IById,
        ISubscriber
    {
        public const string DefaultTableName = "queryUsersById";
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public QueryUsersById(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryUsersById(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _tableName = tableName;
        }

        public async Task<UserDto> ById(string id)
        {
            var entity = await Retrieve(_tableName, id, id).ConfigureAwait(false);
            return entity != null ? entity.GetDto() : null;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(registry.RegisterSubscription<UserCreated>(@event =>
                Insert(new UserDtoTableEntity(new UserDto
                {
                    Id = @event.Id,
                    TeamId = @event.TeamId,
                    CreatedAt = @event.Timestamp
                }), _tableName).Wait()));
            _subscriptionTokens.Add(registry.RegisterSubscription<UserNameUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.Name = @event.Name;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<UserSlackApiAccessTokenUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.SlackApiAccessToken = @event.SlackApiAccessToken;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<UserActivated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = @event.Timestamp;
                Update(_tableName, entity).Wait();
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
