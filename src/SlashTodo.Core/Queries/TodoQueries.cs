using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Core.Queries
{
    public class QueryTodos
    {
        public interface IBySlackConversationId
        {
            Task<TodoDto[]> BySlackConversationId(string slackConversationId);
        }

        public interface IClaimedBySlackUserId
        {
            Task<TodoDto[]> ClaimedBySlackUserId(string slackUserId);
        }

        public interface ICompletedBySlackUserId
        {
            Task<TodoDto[]> CompletedBySlackUserId(string slackUserId, DateTime? since = null, bool includeRemoved = true);
        }
    }
}
