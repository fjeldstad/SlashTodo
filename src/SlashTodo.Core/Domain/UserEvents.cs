using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public abstract class UserEvent : IDomainEvent
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int OriginalVersion { get; set; }
    }

    public class UserCreated : UserEvent
    {
        public Guid AccountId { get; set; }
        public string SlackUserId { get; set; }
    }

    public class UserActivated : UserEvent
    {
    }

    public class UserSlackUserNameUpdated : UserEvent
    {
        public string SlackUserName { get; set; }
    }

    public class UserSlackApiAccessTokenUpdated : UserEvent
    {
        public string SlackApiAccessToken { get; set; }
    }

    public class UserSlackApiAccessTokenRemoved : UserEvent
    {
    }
}
