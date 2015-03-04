using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

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

        public static TodoContext GetContext(Guid? accountId = null, Guid? userId = null)
        {
            return new TodoContext
            {
                AccountId = accountId ?? Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid()
            };
        }

        public static void AssertThatBasicDataIsCorrect(
            this TodoEvent @event, 
            Guid id,
            string slackConversationId,
            TodoContext context, 
            DateTime earliestExpectedTimestamp,
            int? expectedOriginalVersion = null)
        {
            Assert.That(@event, Is.Not.Null);
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.Timestamp, Is.InRange(earliestExpectedTimestamp, DateTime.UtcNow));
            Assert.That(@event.AccountId, Is.EqualTo(context.AccountId));
            Assert.That(@event.SlackConversationId, Is.EqualTo(slackConversationId));
            Assert.That(@event.UserId, Is.EqualTo(context.UserId));
            if (expectedOriginalVersion.HasValue)
            {
                Assert.That(@event.OriginalVersion, Is.EqualTo(expectedOriginalVersion.Value));
            }
        }
    }
}
