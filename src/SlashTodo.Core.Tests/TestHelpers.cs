using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests
{
    public static class TestHelpers
    {
        public static Todo GetTodo(Guid? id = null, TodoContext context = null, TodoData data = null)
        {
            id = id ?? Guid.NewGuid();
            context = context ?? GetContext();
            data = data ?? GetData();
            return new Todo(id.Value, context, data);
        }

        public static TodoContext GetContext(string teamId = "teamId", string conversationId = "conversationId", string userId = "userId")
        {
            return new TodoContext
            {
                TeamId = teamId,
                ConversationId = conversationId,
                UserId = userId
            };
        }

        public static TodoData GetData(string state = "initial", string claimedBy = null, string text = "text")
        {
            return new TodoData
            {
                State = state,
                ClaimedBy = claimedBy,
                Text = text
            };
        }
    }
}
