using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Lookups;
using SlashTodo.Core.Queries;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Queries
{
    public class AzureTableUserQuery : 
        TableStorageBase<AzureTableUserQuery.UserDtoTableEntity>, 
        IUserQuery,
        ISubscriber<UserCreated>,
        ISubscriber<UserSlackUserNameUpdated>,
        ISubscriber<UserSlackApiAccessTokenUpdated>,
        ISubscriber<UserActivated>
    {
        public const string DefaultTableName = "userDtos";
        private readonly string _tableName;
        private readonly IUserLookup _userLookup;

        public string TableName { get { return _tableName; } }

        public AzureTableUserQuery(CloudStorageAccount storageAccount, IUserLookup userLookup)
            : this(storageAccount, DefaultTableName, userLookup)
        {
        }

        public AzureTableUserQuery(CloudStorageAccount storageAccount, string tableName, IUserLookup userLookup)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (userLookup == null)
            {
                throw new ArgumentNullException("userLookup");
            }
            _tableName = tableName;
            _userLookup = userLookup;
        }

        public async Task<Core.Dtos.UserDto> ById(Guid id)
        {
            var entity = await Retrieve(_tableName, id.ToString(), id.ToString()).ConfigureAwait(false);
            return entity != null ? entity.GetUserDto() : null;
        }

        public async Task<Core.Dtos.UserDto> BySlackUserId(string slackUserId)
        {
            var userId = await _userLookup.BySlackUserId(slackUserId).ConfigureAwait(false);
            if (!userId.HasValue)
            {
                return null;
            }
            return await ById(userId.Value).ConfigureAwait(false);
        }

        public async Task HandleEvent(UserCreated @event)
        {
            await Insert(new UserDtoTableEntity(new UserDto
            {
                Id = @event.Id,
                AccountId = @event.AccountId,
                CreatedAt = @event.Timestamp,
                SlackUserId = @event.SlackUserId
            }), _tableName).ConfigureAwait(false);
        }

        public async Task HandleEvent(UserSlackUserNameUpdated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.SlackUserName = @event.SlackUserName;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public async Task HandleEvent(UserSlackApiAccessTokenUpdated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.SlackApiAccessToken = @event.SlackApiAccessToken;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public async Task HandleEvent(UserActivated @event)
        {
            var entity = await Retrieve(_tableName, @event.Id.ToString(), @event.Id.ToString()).ConfigureAwait(false);
            if (entity == null)
            {
                return;
            }
            entity.ActivatedAt = @event.Timestamp;
            await Update(_tableName, entity).ConfigureAwait(false);
        }

        public class UserDtoTableEntity : TableEntity
        {
            public string AccountId { get; set; }
            public string SlackUserId { get; set; }
            public string SlackUserName { get; set; }
            public string SlackApiAccessToken { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ActivatedAt { get; set; }

            public UserDtoTableEntity()
            {
            }

            public UserDtoTableEntity(UserDto dto)
            {
                PartitionKey = dto.Id.ToString();
                RowKey = dto.Id.ToString();
                AccountId = dto.AccountId.ToString();
                SlackUserId = dto.SlackUserId;
                SlackUserName = dto.SlackUserName;
                SlackApiAccessToken = dto.SlackApiAccessToken;
                CreatedAt = dto.CreatedAt;
                ActivatedAt = dto.ActivatedAt;
            }

            public UserDto GetUserDto()
            {
                return new UserDto
                {
                    Id = Guid.Parse(PartitionKey),
                    AccountId = Guid.Parse(AccountId),
                    SlackUserId = SlackUserId,
                    SlackUserName = SlackUserName,
                    SlackApiAccessToken = SlackApiAccessToken,
                    CreatedAt = CreatedAt,
                    ActivatedAt = ActivatedAt
                };
            }
        }
    }
}
