using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Web.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string SlackUserId { get; set; }
        public string SlackUserName { get; set; }
        public string SlackApiAccessToken { get; set; }
    }

    public static class UserExtensions
    {
        public static UserDto ToDto(this Core.Domain.User user)
        {
            if (user == null)
            {
                return null;
            }
            return new UserDto
            {
                Id = user.Id,
                AccountId = user.AccountId,
                SlackUserId = user.SlackUserId,
                SlackUserName = user.SlackUserName,
                SlackApiAccessToken = user.SlackApiAccessToken
            };
        }
    }
}