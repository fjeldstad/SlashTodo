using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Tests.Common;

namespace SlashTodo.Core.Tests.TodoTests
{
    public static class TodoTestHelpers
    {
        public static Todo GetTodo(TodoContext context = null)
        {
            return new Todo()
            {
                Context = context ?? GetContext()
            };
        }

        public static TodoContext GetContext(string teamId = "teamId", string userId = "userId")
        {
            return new TodoContext
            {
                TeamId = teamId,
                UserId = userId
            };
        }

        public static void AssertThatBasicDataIsCorrect(
            this TodoEvent @event, 
            string id,
            string slackConversationId,
            string shortCode,
            TodoContext context, 
            DateTime earliestExpectedTimestamp,
            int? expectedOriginalVersion = null)
        {
            @event.AssertThatBasicEventDataIsCorrect(id, earliestExpectedTimestamp, expectedOriginalVersion);
            Assert.That(@event.TeamId, Is.EqualTo(context.TeamId));
            Assert.That(@event.SlackConversationId, Is.EqualTo(slackConversationId));
            Assert.That(@event.ShortCode, Is.EqualTo(shortCode));
            Assert.That(@event.UserId, Is.EqualTo(context.UserId));
        }
    }
}
