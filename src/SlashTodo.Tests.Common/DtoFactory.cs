using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Tests.Common
{
    public static class DtoFactory
    {
        public static TeamDto Team(
            string id = "teamId",
            string name = "teamName",
            string slashCommandToken = "slashCommandToken",
            Uri slackUrl = null,
            Uri incomingWebhookUrl = null,
            DateTime? createdAt = null,
            DateTime? activatedAt = null)
        {
            return new TeamDto
            {
                Id = id,
                Name = name,
                SlackUrl = slackUrl ?? new Uri("https://name.slack.com"),
                SlashCommandToken = slashCommandToken,
                IncomingWebhookUrl = incomingWebhookUrl ?? new Uri("http://api.slack.com/incoming-webhook/name"),
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-7),
                ActivatedAt = activatedAt ?? DateTime.UtcNow.AddDays(-7).AddSeconds(1)
            };
        }

        public static UserDto User(
            string id = "userId",
            string teamId = "teamId",
            string name = "userName",
            string slackApiAccessToken = "slackApiAccessToken",
            DateTime? createdAt = null,
            DateTime? activatedAt = null)
        {
            return new UserDto
            {
                Id = id,
                TeamId = teamId,
                Name = name,
                SlackApiAccessToken = slackApiAccessToken,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-7),
                ActivatedAt = activatedAt ?? DateTime.UtcNow.AddDays(-7).AddSeconds(1)
            };
        }

        public static TodoDto Todo(
            string id = "todoId",
            string teamId = "teamId",
            string slackConversationId = "slackConversationId",
            string shortCode = "shortCode",
            string text = "text",
            string claimedByUserId = null,
            DateTime? createdAt = null,
            DateTime? completedAt = null,
            DateTime? removedAt = null)
        {
            return new TodoDto
            {
                Id = id,
                TeamId = teamId,
                SlackConversationId = slackConversationId,
                ShortCode = shortCode,
                Text = text,
                ClaimedByUserId = claimedByUserId,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-5),
                CompletedAt = completedAt,
                RemovedAt = removedAt
            };
        }
    }
}
