using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public interface ITodoEvent
    {
        Guid Id { get; set; }
        int OriginalVersion { get; set; }
        string UserId { get; set; }
        DateTime Timestamp { get; set; }
    }
}
