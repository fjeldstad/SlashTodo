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
        private readonly QueryUsers.IById _userQuery;
        private readonly QueryTeams.IById _queryTeamsById;

        public UserMapper(QueryUsers.IById userQuery, QueryTeams.IById queryTeamsById)
        {
            _userQuery = userQuery;
            _queryTeamsById = queryTeamsById;
        }

        public Nancy.Security.IUserIdentity GetUserFromIdentifier(Guid identifier, Nancy.NancyContext context)
        {
            // TODO Handle AggregateExceptions below
            var user = _userQuery.ById(identifier).Result;
            if (user == null)
            {
                return null;
            }
            var account = _queryTeamsById.Query(user.TeamId).Result;
            if (account == null)
            {
                return null; // Ideally this should never happen.
            }
            var userIdentity = new SlackUserIdentity
            {
                Id = user.Id,
                AccountId = user.TeamId,
                SlackUserId = user.SlackUserId,
                SlackUserName = user.Name,
                SlackApiAccessToken = user.SlackApiAccessToken,
                SlackTeamId = account.SlackTeamId,
                SlackTeamName = account.Name,
                SlackTeamUrl = account.SlackUrl
            };
            return userIdentity;
        }
    }
}
