using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public abstract class UserEvent : IDomainEvent
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int OriginalVersion { get; set; }
    }

    public class UserCreated : UserEvent
    {
        public string TeamId { get; set; }
    }

    public class UserNameUpdated : UserEvent
    {
        public string Name { get; set; }
    }

    public class UserSlackApiAccessTokenUpdated : UserEvent
    {
        public string SlackApiAccessToken { get; set; }
    }
}
