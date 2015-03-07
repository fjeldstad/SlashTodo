using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Forms;
using SlashTodo.Core;
using SlashTodo.Core.Queries;

namespace SlashTodo.Web
{
    public class UserMapper : IUserMapper
    {
        private readonly IUserQuery _userQuery;
        private readonly IAccountQuery _accountQuery;

        public UserMapper(IUserQuery userQuery, IAccountQuery accountQuery)
        {
            _userQuery = userQuery;
            _accountQuery = accountQuery;
        }

        public Nancy.Security.IUserIdentity GetUserFromIdentifier(Guid identifier, Nancy.NancyContext context)
        {
            // TODO Handle AggregateExceptions below
            var user = _userQuery.ById(identifier).Result;
            if (user == null)
            {
                return null;
            }
            var account = _accountQuery.ById(user.AccountId).Result;
            if (account == null)
            {
                return null; // Ideally this should never happen.
            }
            var userIdentity = new SlackUserIdentity
            {
                Id = user.Id,
                AccountId = user.AccountId,
                SlackUserId = user.SlackUserId,
                SlackUserName = user.SlackUserName,
                SlackApiAccessToken = user.SlackApiAccessToken,
                SlackTeamId = account.SlackTeamId,
                SlackTeamName = account.SlackTeamName,
                SlackTeamUrl = account.SlackTeamUrl
            };
            return userIdentity;
        }
    }
}
