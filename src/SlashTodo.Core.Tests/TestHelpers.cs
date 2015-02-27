using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests
{
    public static class TestHelpers
    {
        public static Todo GetTodo(TodoContext context = null)
        {
            return new Todo()
            {
                Context = context ?? GetContext()
            };
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

        public static void AssertThatBasicDataIsCorrect(
            this TodoEvent @event, 
            Guid id,
            TodoContext context, 
            DateTime earliestExpectedTimestamp,
            int? expectedOriginalVersion = null)
        {
            Assert.That(@event, Is.Not.Null);
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.Timestamp, Is.InRange(earliestExpectedTimestamp, DateTime.UtcNow));
            Assert.That(@event.UserId, Is.EqualTo(context.UserId));
            if (expectedOriginalVersion.HasValue)
            {
                Assert.That(@event.OriginalVersion, Is.EqualTo(expectedOriginalVersion.Value));
            }
        }
    }
}
