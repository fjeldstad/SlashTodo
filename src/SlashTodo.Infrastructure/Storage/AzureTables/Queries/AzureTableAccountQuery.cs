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
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Queries
{
    public class AzureTableAccountQuery :
        TableStorageBase<AzureTableAccountQuery.AccountDtoTableEntity>,
        IAccountQuery,
        ISubscriber
    {
        public const string DefaultTableName = "accountDtos";
        private readonly string _tableName;
        private readonly IAccountLookup _accountLookup;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

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

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            _subscriptionTokens.Add(registry.RegisterSubscription<AccountCreated>(@event =>
                Insert(new AccountDtoTableEntity(new AccountDto
                {
                    Id = @event.Id,
                    CreatedAt = @event.Timestamp,
                    SlackTeamId = @event.SlackTeamId
                }), _tableName).Wait()));
            _subscriptionTokens.Add(registry.RegisterSubscription<AccountSlackTeamInfoUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
                if (entity == null)
                {
                    return;
                }
                entity.SlackTeamName = @event.SlackTeamName;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<AccountSlashCommandTokenUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
                if (entity == null)
                {
                    return;
                }
                entity.SlashCommandToken = @event.SlashCommandToken;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<AccountIncomingWebhookUpdated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
                if (entity == null)
                {
                    return;
                }
                entity.IncomingWebhookUrl = @event.IncomingWebhookUrl != null ? @event.IncomingWebhookUrl.AbsoluteUri : null;
                Update(_tableName, entity).Wait();
            }));
            _subscriptionTokens.Add(registry.RegisterSubscription<AccountActivated>(@event =>
            {
                var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
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

        public class AccountDtoTableEntity : TableEntity
        {
            public string SlackTeamId { get; set; }
            public string SlackTeamName { get; set; }
            public string SlackTeamUrl { get; set; }
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
                SlackTeamUrl = dto.SlackTeamUrl != null ? dto.SlackTeamUrl.AbsoluteUri : null;
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
                    SlackTeamUrl = !string.IsNullOrEmpty(SlackTeamUrl) ? new Uri(SlackTeamUrl) : null,
                    SlashCommandToken = SlashCommandToken,
                    IncomingWebhookUrl = !string.IsNullOrEmpty(IncomingWebhookUrl) ? new Uri(IncomingWebhookUrl) : null,
                    CreatedAt = CreatedAt,
                    ActivatedAt = ActivatedAt
                };
            }
        }


    }
}
