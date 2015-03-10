using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SlashTodo.Infrastructure.AzureTables;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Web.Security
{
    public class DefaultNancyUserIdentityService : 
        INancyUserIdentityService, 
        IUserMapper
    {
        public const string DefaultUserIdentityByIdTableName = "nancyUserIdentityById";
        public const string DefaultUserIdentityBySlackUserIdTableName = "nancyUserIdentityBySlackUserId";

        private readonly CloudStorageAccount _storageAccount;
        private readonly INancyUserIdentityIdProvider _idProvider;
        private readonly string _userIdentityByIdTableName;
        private readonly string _userIdentityBySlackUserIdTableName;

        public string UserIdentityByIdTableName { get { return _userIdentityByIdTableName; } }
        public string UserIdentityBySlackUserIdTableName { get { return _userIdentityBySlackUserIdTableName; } }

        public DefaultNancyUserIdentityService(
            CloudStorageAccount storageAccount, 
            INancyUserIdentityIdProvider idProvider)
            : this(storageAccount, idProvider, DefaultUserIdentityByIdTableName, DefaultUserIdentityBySlackUserIdTableName)
        {
        }

        public DefaultNancyUserIdentityService(
            CloudStorageAccount storageAccount,
            INancyUserIdentityIdProvider idProvider,
            string userIdentityByIdTableName, 
            string userIdentityBySlackUserIdTableName)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }
            if (idProvider == null)
            {
                throw new ArgumentNullException("idProvider");
            }
            if (string.IsNullOrWhiteSpace(userIdentityByIdTableName))
            {
                throw new ArgumentNullException("userIdentityByIdTableName");
            }
            if (string.IsNullOrWhiteSpace(userIdentityBySlackUserIdTableName))
            {
                throw new ArgumentNullException("userIdentityBySlackUserIdTableName");
            }
            _storageAccount = storageAccount;
            _idProvider = idProvider;
            _userIdentityByIdTableName = userIdentityByIdTableName;
            _userIdentityBySlackUserIdTableName = userIdentityBySlackUserIdTableName;
        }

        public async Task<NancyUserIdentity> GetOrCreate(string slackUserId, string slackTeamId, string slackUserName)
        {
            var table = await _storageAccount.GetTableAsync(_userIdentityBySlackUserIdTableName).ConfigureAwait(false);
            var entity = await table.RetrieveAsync<NancyUserIdentityTableEntity>(slackUserId, slackUserId).ConfigureAwait(false);
            if (entity != null)
            {
                return entity.GetUserIdentity();
            }
            var userIdentity = new NancyUserIdentity
            {
                Id = _idProvider.GenerateNewId(),
                SlackUserId = slackUserId,
                SlackTeamId = slackTeamId,
                SlackUserName = slackUserName
            };
            var insertBySlackUserIdTask = table.InsertAsync(new NancyUserIdentityTableEntity(userIdentity, x => x.SlackUserId));
            insertBySlackUserIdTask.ConfigureAwait(false);
            table = await _storageAccount.GetTableAsync(_userIdentityByIdTableName).ConfigureAwait(false);
            var insertByIdTask = table.InsertAsync(new NancyUserIdentityTableEntity(userIdentity, x => x.Id.ToString()));
            insertByIdTask.ConfigureAwait(false);
            await Task.WhenAll(insertBySlackUserIdTask, insertByIdTask).ConfigureAwait(false);
            return userIdentity;
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, Nancy.NancyContext context)
        {
            var table = _storageAccount.GetTable(_userIdentityByIdTableName);
            var entity = table.Retrieve<NancyUserIdentityTableEntity>(identifier.ToString(), identifier.ToString());
            return entity != null ? entity.GetUserIdentity() : null;
        }

        public class NancyUserIdentityTableEntity : TableEntity
        {
            public string Id { get; set; }
            public string SlackUserId { get; set; }
            public string SlackTeamId { get; set; }
            public string SlackUserName { get; set; }

            public NancyUserIdentityTableEntity()
            {
            }

            public NancyUserIdentityTableEntity(NancyUserIdentity userIdentity, Func<NancyUserIdentity, string> key)
            {
                PartitionKey = key(userIdentity);
                RowKey = key(userIdentity);

                Id = userIdentity.Id.ToString();
                SlackUserId = userIdentity.SlackUserId;
                SlackTeamId = userIdentity.SlackTeamId;
                SlackUserName = userIdentity.SlackUserName;
            }

            public NancyUserIdentity GetUserIdentity()
            {
                return new NancyUserIdentity
                {
                    Id = Guid.Parse(Id),
                    SlackUserId = SlackUserId,
                    SlackTeamId = SlackTeamId,
                    SlackUserName = SlackUserName
                };
            }
        }
    }
}