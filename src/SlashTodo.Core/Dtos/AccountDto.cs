using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Core.Dtos
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string SlackTeamId { get; set; }
        public string SlackTeamName { get; set; }
        public string SlashCommandToken { get; set; }
        public Uri IncomingWebhookUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool IsActive { get { return ActivatedAt.HasValue; } }
    }
}