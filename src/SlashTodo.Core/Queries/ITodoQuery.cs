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
        Task<TodoDto[]> BySlackConversationId(string slackConversationId);
        Task<TodoDto[]> ClaimedBySlackUserId(string slackUserId);
        Task<TodoDto[]> CompletedBySlackUserId(string slackUserId, DateTime? since = null, bool includeRemoved = true);
    }
}
