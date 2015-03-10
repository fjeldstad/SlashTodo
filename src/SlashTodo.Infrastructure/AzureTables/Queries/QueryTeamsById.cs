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
        QueryTeams.IById,
        ISubscriber
    {
        public const string DefaultTableName = "queryTeamsById";

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string TableName { get { return _tableName; } }

        public QueryTeamsById(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryTeamsById(CloudStorageAccount storageAccount, string tableName)
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

        public async Task<TeamDto> ById(string id)
        {
            var table = await _storageAccount.GetTableAsync(_tableName).ConfigureAwait(false);
            var entity = await table.RetrieveAsync<TeamDtoTableEntity>(id, id).ConfigureAwait(false);
            return entity != null ? entity.GetDto() : null;
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamCreated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                table.Insert(new TeamDtoTableEntity(new TeamDto
                {
                    Id = @event.Id,
                    CreatedAt = @event.Timestamp
                }));
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamInfoUpdated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.Name = @event.Name;
                entity.SlackUrl = @event.SlackUrl != null ? @event.SlackUrl.AbsoluteUri : null;
                table.Update(entity);
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamSlashCommandTokenUpdated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.SlashCommandToken = @event.SlashCommandToken;
                table.Update(entity);
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamIncomingWebhookUpdated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.IncomingWebhookUrl = @event.IncomingWebhookUrl != null ? @event.IncomingWebhookUrl.AbsoluteUri : null;
                table.Update(entity);
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamActivated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = @event.Timestamp;
                table.Update(entity);
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<TeamDeactivated>(@event =>
            {
                var table = _storageAccount.GetTable(_tableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = null;
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
