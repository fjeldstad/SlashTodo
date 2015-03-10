using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public abstract class TeamEvent : IDomainEvent
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int OriginalVersion { get; set; }
    }

    public class TeamCreated : TeamEvent
    {
    }

    public class TeamSlashCommandTokenUpdated : TeamEvent
    {
        public string SlashCommandToken { get; set; }
    }

    public class TeamIncomingWebhookUpdated : TeamEvent
    {
        public Uri IncomingWebhookUrl { get; set; }
    }

    public class TeamInfoUpdated : TeamEvent
    {
        public string Name { get; set; }
        public Uri SlackUrl { get; set; }
    }

    public class TeamActivated : TeamEvent
    {
    }

    public class TeamDeactivated : TeamEvent
    {
    }
}
