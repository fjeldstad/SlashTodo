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

        //public interface IClaimedByUserId
        //{
        //    Task<TodoDto[]> ClaimedBySlackUserId(string userId);
        //}

        //public interface ICompletedByUserId
        //{
        //    Task<TodoDto[]> CompletedByUserId(string userId, DateTime? since = null, bool includeRemoved = true);
        //}
    }
}
