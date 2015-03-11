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
        QueryBase,
        QueryTeams.IById
    {
        public const string DefaultTableName = "queryTeamsById";

        public QueryTeamsById(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableName)
        {
        }

        public QueryTeamsById(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount, tableName)
        {
        }

        public async Task<TeamDto> ById(string id)
        {
            var table = await StorageAccount.GetTableAsync(TableName).ConfigureAwait(false);
            var entity = await table.RetrieveAsync<TeamDtoTableEntity>(id, id).ConfigureAwait(false);
            return entity != null ? entity.GetDto() : null;
        }

        protected override IEnumerable<ISubscriptionToken> RegisterSubscriptionsCore(ISubscriptionRegistry registry)
        {
            yield return registry.RegisterSubscription<TeamCreated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                table.Insert(new TeamDtoTableEntity(new TeamDto
                {
                    Id = @event.Id,
                    CreatedAt = @event.Timestamp
                }));
            });
            yield return registry.RegisterSubscription<TeamInfoUpdated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.Name = @event.Name;
                entity.SlackUrl = @event.SlackUrl != null ? @event.SlackUrl.AbsoluteUri : null;
                table.Update(entity);
            });
            yield return registry.RegisterSubscription<TeamSlashCommandTokenUpdated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.SlashCommandToken = @event.SlashCommandToken;
                table.Update(entity);
            });
            yield return registry.RegisterSubscription<TeamIncomingWebhookUpdated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.IncomingWebhookUrl = @event.IncomingWebhookUrl != null ? @event.IncomingWebhookUrl.AbsoluteUri : null;
                table.Update(entity);
            });
            yield return registry.RegisterSubscription<TeamActivated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = @event.Timestamp;
                table.Update(entity);
            });
            yield return registry.RegisterSubscription<TeamDeactivated>(@event =>
            {
                var table = StorageAccount.GetTable(TableName);
                var entity = table.Retrieve<TeamDtoTableEntity>(@event.Id, @event.Id);
                if (entity == null)
                {
                    return;
                }
                entity.ActivatedAt = null;
                table.Update(entity);
            });
        }
    }
}
