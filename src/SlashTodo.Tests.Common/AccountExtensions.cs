using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Tests.Common
{
    public static class AccountExtensions
    {
        public static AccountDto ToDto(this Core.Domain.Account account, DateTime createdAt, DateTime? activatedAt = null)
        {
            if (account == null)
            {
                return null;
            }
            return new AccountDto
            {
                Id = account.Id,
                SlackTeamId = account.SlackTeamId,
                SlackTeamName = account.SlackTeamName,
                SlashCommandToken = account.SlashCommandToken,
                IncomingWebhookUrl = account.IncomingWebhookUrl,
                CreatedAt = createdAt,
                ActivatedAt = activatedAt
            };
        }
    }
}
