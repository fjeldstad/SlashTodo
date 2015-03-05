using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Web.Dtos
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string SlackTeamId { get; set; }
        public string SlackTeamName { get; set; }
        public string SlashCommandToken { get; set; }
        public Uri IncomingWebhookUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public static class AccountExtensions
    {
        public static AccountDto ToDto(this Core.Domain.Account account)
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
                IsActive = account.IsActive
            };
        }
    }
}