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
        public string TeamId { get; set; }
    }
}
