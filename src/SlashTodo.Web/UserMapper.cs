using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Forms;
using SlashTodo.Core;

namespace SlashTodo.Web
{
    public class UserMapper : IUserMapper
    {
        // TODO Use a read model (view) instead, for performance.
        private readonly IRepository<Core.Domain.User> _userRepository;

        public UserMapper(IRepository<Core.Domain.User> userRepository)
        {
            _userRepository = userRepository;
        }

        public Nancy.Security.IUserIdentity GetUserFromIdentifier(Guid identifier, Nancy.NancyContext context)
        {
            var user = _userRepository.GetById(identifier).Result; // TODO Await?
            if (user == null)
            {
                return null;
            }
            var userIdentity = new SlackUserIdentity
            {
                Id = user.Id,
                AccountId = user.AccountId,
                SlackUserId = user.SlackUserId,
                SlackUserName = user.SlackUserName,
                SlackApiAccessToken = user.SlackApiAccessToken
            };
            return userIdentity;
        }
    }
}
