using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Tests.Common
{
    public static class UserExtensions
    {
        public static UserDto ToDto(this Core.Domain.User user, DateTime createdAt, bool isActive = false)
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
                SlackApiAccessToken = user.SlackApiAccessToken,
                CreatedAt = createdAt,
                IsActive = isActive
            };
        }
    }
}
