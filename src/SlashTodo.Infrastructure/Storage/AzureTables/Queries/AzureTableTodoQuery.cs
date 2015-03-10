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
    public class AzureTableTodoQuery :
        TableStorageBase<AzureTableTodoQuery.TodoDtoTableEntity>,
        ITodoQuery,
        ISubscriber
    {
        public static readonly TodoQueryTableNames DefaultTableNames = new TodoQueryTableNames
        {
            TodoBySlackConversationId = "todoDtosBySlackConversationId",
            TodoClaimedBySlackUserId = "todoDtosClaimedBySlackUserId",
            TodoCompletedBySlackUserId = "todoDtosCompletedBySlackUserId"
        };

        private readonly TodoQueryTableNames _tableNames;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public TodoQueryTableNames TableNames { get { return _tableNames; } }

        public AzureTableTodoQuery(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultTableNames)
        {
        }

        public AzureTableTodoQuery(CloudStorageAccount storageAccount, TodoQueryTableNames tableNames)
            : base(storageAccount)
        {
            if (tableNames == null)
            {
                throw new ArgumentNullException("tableNames");
            }
            if (!tableNames.IsValid())
            {
                throw new ArgumentException("Invalid table names configuration.", "tableNames");
            }
            _tableNames = tableNames;
        }

        public Task<TodoDto[]> BySlackConversationId(string slackConversationId)
        {
            throw new NotImplementedException();
        }

        public Task<TodoDto[]> ClaimedBySlackUserId(string slackUserId)
        {
            throw new NotImplementedException();
        }

        public Task<TodoDto[]> CompletedBySlackUserId(string slackUserId, DateTime? since = null, bool includeRemoved = true)
        {
            throw new NotImplementedException();
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            //_subscriptionTokens.Add(registry.RegisterSubscription<AccountCreated>(@event =>
            //    Insert(new AccountDtoTableEntity(new AccountDto
            //    {
            //        Id = @event.Id,
            //        CreatedAt = @event.Timestamp,
            //        SlackTeamId = @event.SlackTeamId
            //    }), _tableName).Wait()));
            //_subscriptionTokens.Add(registry.RegisterSubscription<AccountSlackTeamInfoUpdated>(@event =>
            //{
            //    var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
            //    if (entity == null)
            //    {
            //        return;
            //    }
            //    entity.SlackTeamName = @event.SlackTeamName;
            //    entity.SlackTeamUrl = @event.SlackTeamUrl != null ? @event.SlackTeamUrl.AbsoluteUri : null;
            //    Update(_tableName, entity).Wait();
            //}));
            //_subscriptionTokens.Add(registry.RegisterSubscription<AccountSlashCommandTokenUpdated>(@event =>
            //{
            //    var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
            //    if (entity == null)
            //    {
            //        return;
            //    }
            //    entity.SlashCommandToken = @event.SlashCommandToken;
            //    Update(_tableName, entity).Wait();
            //}));
            //_subscriptionTokens.Add(registry.RegisterSubscription<AccountIncomingWebhookUpdated>(@event =>
            //{
            //    var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
            //    if (entity == null)
            //    {
            //        return;
            //    }
            //    entity.IncomingWebhookUrl = @event.IncomingWebhookUrl != null ? @event.IncomingWebhookUrl.AbsoluteUri : null;
            //    Update(_tableName, entity).Wait();
            //}));
            //_subscriptionTokens.Add(registry.RegisterSubscription<AccountActivated>(@event =>
            //{
            //    var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
            //    if (entity == null)
            //    {
            //        return;
            //    }
            //    entity.IsActive = true;
            //    Update(_tableName, entity).Wait();
            //}));
            //_subscriptionTokens.Add(registry.RegisterSubscription<AccountDeactivated>(@event =>
            //{
            //    var entity = Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).Result;
            //    if (entity == null)
            //    {
            //        return;
            //    }
            //    entity.IsActive = false;
            //    Update(_tableName, entity).Wait();
            //}));
        }

        public void Dispose()
        {
            foreach (var token in _subscriptionTokens)
            {
                token.Dispose();
            }
        }

        public class TodoQueryTableNames
        {
            public string TodoBySlackConversationId { get; set; }
            public string TodoClaimedBySlackUserId { get; set; }
            public string TodoCompletedBySlackUserId { get; set; }

            public bool IsValid()
            {
                return !string.IsNullOrWhiteSpace(TodoBySlackConversationId) &&
                       !string.IsNullOrWhiteSpace(TodoClaimedBySlackUserId) &&
                       !string.IsNullOrWhiteSpace(TodoCompletedBySlackUserId);
            }
        }

        public class TodoDtoTableEntity : TableEntity
        {
            public string Id { get; set; }
            public string AccountId { get; set; }
            public string SlackConversationId { get; set; }
            public string ShortCode { get; set; }
            public string Text { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
            public DateTime? RemovedAt { get; set; }
            public string ClaimedByUserId { get; set; }
            public string ClaimedBySlackUserId { get; set; }

            public TodoDtoTableEntity()
            {
            }

            public TodoDtoTableEntity(TodoDto dto, Func<TodoDto, string> partitionKey, Func<TodoDto, string> rowKey)
            {
                PartitionKey = partitionKey(dto);
                RowKey = rowKey(dto);

                Id = dto.Id.ToString();
                AccountId = dto.AccountId.ToString();
                SlackConversationId = dto.SlackConversationId;
                ShortCode = dto.ShortCode;
                Text = dto.Text;
                CreatedAt = dto.CreatedAt;
                CompletedAt = dto.CompletedAt;
                RemovedAt = dto.RemovedAt;
                ClaimedByUserId = dto.ClaimedByUserId.HasValue ? dto.ClaimedByUserId.ToString() : null;
                ClaimedBySlackUserId = dto.ClaimedBySlackUserId;
            }

            public TodoDto GetTodoDto()
            {
                return new TodoDto
                {
                    Id = Guid.Parse(Id),
                    AccountId = Guid.Parse(AccountId),
                    SlackConversationId = SlackConversationId,
                    ShortCode = ShortCode,
                    Text = Text,
                    CreatedAt = CreatedAt,
                    CompletedAt = CompletedAt,
                    RemovedAt = RemovedAt,
                    ClaimedByUserId = !string.IsNullOrEmpty(ClaimedByUserId) ? Guid.Parse(ClaimedByUserId) : (Guid?)null,
                    ClaimedBySlackUserId = ClaimedBySlackUserId
                };
            }
        }
    }
}
