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
    public class ClaimTodoTests
    {
        [Test]
        public void CanClaimTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var slackConversationId = "slackConversationId";
            var todo = Todo.Add(id, "text", slackConversationId, context);
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Claim();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoClaimed;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void ClaimTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", context);
            todo.ClearUncommittedEvents();

            // Act
            todo.Claim();
            todo.Claim();
            todo.Claim();

            // Assert
            Assert.That(todo.GetUncommittedEvents().Count(), Is.EqualTo(1));
        }

        [Test]
        public void CannotClaimTodoThatIsClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var todo = Todo.Add(id, "text", "slackConversationId", otherUserContext);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));

            // Act & assert
            todo.Context = TodoTestHelpers.GetContext(userId: userId);
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Claim(), 
                ex => ex.ClaimedByUserId == otherUserContext.UserId);
        }

        [Test]
        public void CanClaimTodoThatIsClaimedBySomeoneElseWhenUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var slackConversationId = "slackConversationId";
            var todo = Todo.Add(id, "text", slackConversationId, otherUserContext);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;
            

            // Act
            var context = todo.Context = TodoTestHelpers.GetContext(userId: userId);
            todo.Claim(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoClaimed;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void ClaimingRemovedTodoDoesNothing()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", context);
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Claim();

            // Assert
            Assert.That(todo.GetUncommittedEvents(), Is.Empty);
        }

        [Test]
        public void ClaimingTickedTodoDoesNothing()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, "text", "slackConversationId", context);
            todo.Tick();
            todo.ClearUncommittedEvents();

            // Act
            todo.Claim();

            // Assert
            Assert.That(todo.GetUncommittedEvents(), Is.Empty);
        }
    }
}
