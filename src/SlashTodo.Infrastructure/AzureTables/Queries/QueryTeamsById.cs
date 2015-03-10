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
    public class QueryTeamsById :
        TableStorageBase<TeamDtoTableEntity>,
        QueryTeams.IById,
        ISubscriber
    {
        public const string DefaultTableName = "queryTeamsById";
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public QueryTeamsById(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryTeamsById(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _tableName = tableName;
        }

        public async Task<TeamDto> ById(string id)
        {
            var entity = await Retrieve(_tableName, id, id).ConfigureAwait(false);
            return entity != null ? entity.GetDto() : null;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamCreated>(@event =>
                Insert(new TeamDtoTableEntity(new TeamDto
                {
                    Id = @event.Id,
                    CreatedAt = @event.Timestamp
                }), _tableName).Wait()));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamInfoUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.Name = @event.Name;
                entity.SlackUrl = @event.SlackUrl != null ? @event.SlackUrl.AbsoluteUri : null;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamSlashCommandTokenUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.SlashCommandToken = @event.SlashCommandToken;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamIncomingWebhookUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.IncomingWebhookUrl = @event.IncomingWebhookUrl != null ? @event.IncomingWebhookUrl.AbsoluteUri : null;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamActivated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = @event.Timestamp;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamDeactivated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id, @event.Id).Result;
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = null;
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
