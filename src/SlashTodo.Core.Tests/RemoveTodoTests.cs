using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests
{
    [TestFixture]
    public class RemoveTodoTests
    {
        [Test]
        public void CanRemoveTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Remove();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoRemoved;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void RemoveTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.ClearUncommittedEvents();

            // Act
            todo.Remove();
            todo.Remove();
            todo.Remove();

            // Assert
            Assert.That(todo.GetUncommittedEvents().Count(), Is.EqualTo(1));
        }

        [Test]
        public void CannotRemoveTodoThatIsClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserContext = TestHelpers.GetContext(userId: "otherUserId");
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act & assert
            todo.Context = TestHelpers.GetContext();
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Remove(),
                ex => ex.ClaimedBy == otherUserContext.UserId);
        }

        [Test]
        public void CanRemoveTodoThatIsClaimedBySomeoneElseWhenUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserContext = TestHelpers.GetContext(userId: "otherUserId");
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            var context = todo.Context = TestHelpers.GetContext();
            todo.Remove(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoRemoved;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void CanRemoveTodoThatIsClaimedBySameUserWithoutUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Remove();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoRemoved;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }
    }
}
