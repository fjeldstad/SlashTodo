using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public interface IDomainEvent
    {
        Guid Id { get; set; }
        DateTime Timestamp { get; set; }
        int OriginalVersion { get; set; }
    }
}
