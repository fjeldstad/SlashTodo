using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public abstract class AccountEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int OriginalVersion { get; set; }
    }

    public class AccountCreated : AccountEvent
    {
        public string SlackTeamId { get; set; }
    }

    public class AccountSlashCommandTokenUpdated : AccountEvent
    {
        public string SlashCommandToken { get; set; }
    }

    public class AccountIncomingWebhookUpdated : AccountEvent
    {
        public Uri IncomingWebhookUrl { get; set; }
    }

    public class AccountSlackTeamInfoUpdated : AccountEvent
    {
        public string SlackTeamName { get; set; }
        public Uri SlackTeamUrl { get; set; }
    }

    public class AccountActivated : AccountEvent
    {
    }

    public class AccountDeactivated : AccountEvent
    {
    }
}
