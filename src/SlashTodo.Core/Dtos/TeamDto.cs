using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Core.Dtos
{
    public class TeamDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Uri SlackUrl { get; set; }
        public string SlashCommandToken { get; set; }
        public Uri IncomingWebhookUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool IsActive { get { return ActivatedAt.HasValue; } }
    }
}