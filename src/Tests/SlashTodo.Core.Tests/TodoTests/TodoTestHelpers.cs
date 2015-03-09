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
            string shortCode,
            TodoContext context, 
            DateTime earliestExpectedTimestamp,
            int? expectedOriginalVersion = null)
        {
            @event.AssertThatBasicEventDataIsCorrect(id, earliestExpectedTimestamp, expectedOriginalVersion);
            Assert.That(@event.AccountId, Is.EqualTo(context.AccountId));
            Assert.That(@event.SlackConversationId, Is.EqualTo(slackConversationId));
            Assert.That(@event.ShortCode, Is.EqualTo(shortCode));
            Assert.That(@event.UserId, Is.EqualTo(context.UserId));
        }
    }
}
