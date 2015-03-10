using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.TodoTests
{
    [TestFixture]
    public class UntickTodoTests
    {
        [Test]
        public void CanUntickTodo()
        {
            // Arrange
            var id = "id";
            var context = TodoTestHelpers.GetContext();
            var slackConversationId = "slackConversationId";
            var shortCode = "x";
            var todo = Todo.Add(id, "text", slackConversationId, shortCode, context);
            todo.Tick();
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Untick();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoUnticked;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, shortCode, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void UntickTodoIsIdempotentOperation()
        {
            // Arrange
            var id = "id";
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", "x", context);
            todo.Tick();
            todo.ClearUncommittedEvents();

            // Act
            todo.Untick();
            todo.Untick();
            todo.Untick();

            // Assert
            Assert.That(todo.GetUncommittedEvents().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UntickingRemovedTodoDoesNothing()
        {
            // Arrange
            var id = "id";
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", "x", context);
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Untick();

            // Assert
            Assert.That(todo.GetUncommittedEvents(), Is.Empty);
        }
    }
}
