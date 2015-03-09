using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Core.Queries
{
    public interface ITodoQuery
    {
        Task<TodoDto[]> BySlackConversationId(string slackConversationId, bool includeRemoved = false);
        Task<TodoDto[]> ClaimedByUserId(Guid userId);
        Task<TodoDto[]> CompletedByUserId(Guid userId, DateTime? since = null, bool includeRemoved = true);
    }
}
