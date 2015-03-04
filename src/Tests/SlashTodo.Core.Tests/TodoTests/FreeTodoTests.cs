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
    public class FreeTodoTests
    {
        [Test]
        public void CanFreeTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var slackConversationId = "slackConversationId";
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", slackConversationId, context);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Free();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoFreed;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void FreeTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var slackConversationId = "slackConversationId";
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", slackConversationId, context);
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act
            todo.Free();
            todo.Free();
            todo.Free();

            // Assert
            Assert.That(todo.GetUncommittedEvents().Count(), Is.EqualTo(1));
        }

        [Test]
        public void CannotFreeTodoThatIsClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var todo = Todo.Add(id, "text", "slackConversationId", otherUserContext);
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act & assert
            todo.Context = TodoTestHelpers.GetContext(userId);
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Free(),
                ex => ex.ClaimedByUserId == otherUserContext.UserId);
        }

        [Test]
        public void CanFreeTodoThatIsClaimedBySomeoneElseWhenUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));
            var slackConversationId = "slackConversationId";
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var todo = Todo.Add(id, "text", slackConversationId, otherUserContext);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            var context = todo.Context = TodoTestHelpers.GetContext(userId: userId);
            todo.Free(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoFreed;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, context, before, expectedOriginalVersion: originalVersion);
        }
    }
}
