using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Lookups;
using SlashTodo.Core.Queries;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Queries
{
    public class AzureTableAccountQuery :
        TableStorageBase<AzureTableAccountQuery.AccountDtoTableEntity>,
        IAccountQuery,
        ISubscriber<AccountCreated>,
        ISubscriber<AccountSlackTeamNameUpdated>,
        ISubscriber<AccountSlashCommandTokenUpdated>,
        ISubscriber<AccountIncomingWebhookUpdated>,
        ISubscriber<AccountActivated>
    {
        public const string DefaultTableName = "accountDtos";
        private readonly string _tableName;
        private readonly IAccountLookup _accountLookup;

        public string TableName { get { return _tableName; } }

        public AzureTableAccountQuery(CloudStorageAccount storageAccount, IAccountLookup accountLookup)
            : this(storageAccount, DefaultTableName, accountLookup)
        {
        }

        public AzureTableAccountQuery(CloudStorageAccount storageAccount, string tableName, IAccountLookup accountLookup)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (accountLookup == null)
            {
                throw new ArgumentNullException("accountLookup");
            }
            _tableName = tableName;
            _accountLookup = accountLookup;
        }

        public async Task<Core.Dtos.AccountDto> ById(Guid id)
        {
            var entity = await Retrieve(_tableName, id.ToString(), id.ToString()).ConfigureAwait(false);
            return entity != null ? entity.GetAccountDto() : null;
        }

        public async Task<Core.Dtos.AccountDto> BySlackTeamId(string slackTeamId)
        {
            var accountId = await _accountLookup.BySlackTeamId(slackTeamId).ConfigureAwait(false);
            if (!accountId.HasValue)
            {
                return null;
            }
            return await ById(accountId.Value).ConfigureAwait(false);
        }

        public async Task HandleEvent(AccountCreated @event)
        {
            await Insert(new AccountDtoTableEntity(new AccountDto
            {
                Id = @event.Id,
                CreatedAt = @event.Timestamp,
                SlackTeamId = @event.SlackTeamId
            }), _tableName).ConfigureAwait(false);
        }

        public async Task HandleEvent(AccountSlackTeamNameUpdated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.SlackTeamName = @event.SlackTeamName;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public async Task HandleEvent(AccountSlashCommandTokenUpdated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.SlashCommandToken = @event.SlashCommandToken;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public async Task HandleEvent(AccountIncomingWebhookUpdated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.IncomingWebhookUrl = @event.IncomingWebhookUrl != null ? @event.IncomingWebhookUrl.AbsoluteUri : null;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public async Task HandleEvent(AccountActivated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.ActivatedAt = @event.Timestamp;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public class AccountDtoTableEntity : TableEntity
        {
            public string SlackTeamId { get; set; }
            public string SlackTeamName { get; set; }
            public string SlashCommandToken { get; set; }
            public string IncomingWebhookUrl { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ActivatedAt { get; set; }

            public AccountDtoTableEntity()
            {
            }

            public AccountDtoTableEntity(AccountDto dto)
            {
                PartitionKey = dto.Id.ToString();
                RowKey = dto.Id.ToString();
                SlackTeamId = dto.SlackTeamId;
                SlackTeamName = dto.SlackTeamName;
                SlashCommandToken = dto.SlashCommandToken;
                IncomingWebhookUrl = dto.IncomingWebhookUrl != null ? dto.IncomingWebhookUrl.AbsoluteUri : null;
                CreatedAt = dto.CreatedAt;
                ActivatedAt = dto.ActivatedAt;
            }

            public AccountDto GetAccountDto()
            {
                return new AccountDto
                {
                    Id = Guid.Parse(PartitionKey),
                    SlackTeamId = SlackTeamId,
                    SlackTeamName = SlackTeamName,
                    SlashCommandToken = SlashCommandToken,
                    IncomingWebhookUrl = !string.IsNullOrEmpty(IncomingWebhookUrl) ? new Uri(IncomingWebhookUrl) : null,
                    CreatedAt = CreatedAt,
                    ActivatedAt = ActivatedAt
                };
            }
        }
    }
}
