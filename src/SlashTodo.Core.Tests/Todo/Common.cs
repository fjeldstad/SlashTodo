using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.Todo
{
    public static class Common
    {
        public static TodoContext GetContext(
            string teamId = "teamId",
            string conversationId = "conversationId",
            string userId = "userId")
        {
            return new TodoContext
            {
                TeamId = teamId,
                ConversationId = conversationId,
                UserId = userId
            };
        }

        public static Core.Todo GetTodo(TodoContext context = null)
        {
            if (context == null)
            {
                context = GetContext();
            }
            var todo = new Core.Todo(context, Guid.NewGuid(), "text");
            todo.ClearUncommittedEvents();
            return todo;
        }

        public static Core.Todo GetTodoClaimedBySomeoneElse(TodoContext context = null)
        {
            if (context == null)
            {
                context = GetContext();
            }
            var todo = new Core.Todo(context, Guid.NewGuid(), "text");
            var originalUserId = context.UserId;
            context.UserId = "someoneOtherThan_" + originalUserId;
            todo.Claim();
            var todoClaimed = todo.GetUncommittedEvents().Last() as TodoClaimed;
            Assert.That(todoClaimed != null);
            Assert.That(todoClaimed.UserId, Is.Not.EqualTo(originalUserId));
            context.UserId = originalUserId;
            todo.ClearUncommittedEvents();
            return todo;
        }
    }
}
