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
    [TestFixture]
    public class TickTodoTests
    {
        [Test]
        public void CanTickTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var slackConversationId = "slackConversationId";
            var shortCode = "x";
            var todo = Todo.Add(id, "text", slackConversationId, shortCode, context);
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Tick();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoTicked;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, shortCode, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void TickTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", "x", context);
            todo.ClearUncommittedEvents();

            // Act
            todo.Tick();
            todo.Tick();
            todo.Tick();

            // Assert
            Assert.That(todo.GetUncommittedEvents().Count(), Is.EqualTo(1));
        }

        [Test]
        public void TickingRemovedTodoDoesNothing()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", "x", context);
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Tick();

            // Assert
            Assert.That(todo.GetUncommittedEvents(), Is.Empty);
        }

        [Test]
        public void CannotTickTodoThatIsClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var todo = Todo.Add(id, "text", "slackConversationId", "x", otherUserContext);
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act & assert
            todo.Context = TodoTestHelpers.GetContext(userId: userId);
            CustomAssert.Throws<TodoClaimedBySomeoneElseException>(
                () => todo.Tick(),
                ex => ex.ClaimedByUserId == otherUserContext.UserId);
        }

        [Test]
        public void CanTickTodoThatIsClaimedBySomeoneElseWhenUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var slackConversationId = "slackConversationId";
            var shortCode = "x";
            var todo = Todo.Add(id, "text", slackConversationId, shortCode, otherUserContext);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            var context = todo.Context = TodoTestHelpers.GetContext(userId: userId);
            todo.Tick(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoTicked;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, shortCode, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void CanTickTodoThatIsClaimedBySameUserWithoutUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var slackConversationId = "slackConversationId";
            var shortCode = "x";
            var todo = Todo.Add(id, "text", slackConversationId, shortCode, context);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Tick();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoTicked;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, shortCode, context, before, expectedOriginalVersion: originalVersion);
        }
    }
}
