using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Lookups
{
    public interface ITodoLookup
    {
        Task<Guid?> BySlackConversationIdAndShortCode(string slackConversationId, string shortCode);
    }
}
