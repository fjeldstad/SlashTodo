using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SlashTodo.Infrastructure.Messaging;

namespace SlashTodo.Web.Security
{
    public class DefaultNancyUserIdentityService : 
        INancyUserIdentityService, 
        IUserMapper,
        ISubscriber
    {
        public const string DefaultUserIdentityByIdTableName = "nancyUserIdentityById";
        public const string DefaultUserIdentityBySlackUserIdTableName = "nancyUserIdentityBySlackUserId";

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _userIdentityByIdTableName;
        private readonly string _userIdentityBySlackUserIdTableName;
        private readonly List<ISubscriptionToken> _subscriptionTokens = new List<ISubscriptionToken>();

        public string UserIdentityByIdTableName { get { return _userIdentityByIdTableName; } }
        public string UserIdentityBySlackUserIdTableName { get { return _userIdentityBySlackUserIdTableName; } }

        public DefaultNancyUserIdentityService(CloudStorageAccount storageAccount)
            : this(storageAccount, DefaultUserIdentityByIdTableName, DefaultUserIdentityBySlackUserIdTableName)
        {
        }

        public DefaultNancyUserIdentityService(
            CloudStorageAccount storageAccount, 
            string userIdentityByIdTableName, 
            string userIdentityBySlackUserIdTableName)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
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
            _userIdentityByIdTableName = userIdentityByIdTableName;
            _userIdentityBySlackUserIdTableName = userIdentityBySlackUserIdTableName;
        }

        public Task<NancyUserIdentity> GetOrCreate(string slackUserId, string slackTeamId, string slackUserName)
        {
            // TODO
            throw new NotImplementedException();
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, Nancy.NancyContext context)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void RegisterSubscriptions(ISubscriptionRegistry registry)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            foreach (var token in _subscriptionTokens)
            {
                token.Dispose();
            }
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