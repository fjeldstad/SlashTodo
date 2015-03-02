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
            var todo = Todo.Add(id, context, "text");
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Claim();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoClaimed;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void ClaimTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
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
            var otherUserContext = TodoTestHelpers.GetContext(userId: "otherUserId");
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act & assert
            todo.Context = TodoTestHelpers.GetContext();
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Claim(), 
                ex => ex.ClaimedBy == otherUserContext.UserId);
        }

        [Test]
        public void CanClaimTodoThatIsClaimedBySomeoneElseWhenUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserContext = TodoTestHelpers.GetContext(userId: "otherUserId");
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            var context = todo.Context = TodoTestHelpers.GetContext();
            todo.Claim(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoClaimed;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void ClaimingRemovedTodoDoesNothing()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
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
            var todo = Todo.Add(id, context, "text");
            todo.Tick();
            todo.ClearUncommittedEvents();

            // Act
            todo.Claim();

            // Assert
            Assert.That(todo.GetUncommittedEvents(), Is.Empty);
        }
    }
}
