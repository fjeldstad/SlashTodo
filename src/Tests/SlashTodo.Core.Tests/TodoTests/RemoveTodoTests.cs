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
    public class RemoveTodoTests
    {
        [Test]
        public void CanRemoveTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
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
            var context = TodoTestHelpers.GetContext();
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
            var otherUserId = Guid.NewGuid();
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));

            // Act & assert
            todo.Context = TodoTestHelpers.GetContext(userId: userId);
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Remove(),
                ex => ex.ClaimedByUserId == otherUserContext.UserId);
        }

        [Test]
        public void CanRemoveTodoThatIsClaimedBySomeoneElseWhenUsingForce()
        {
            // Arrange
            var id = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var otherUserContext = TodoTestHelpers.GetContext(userId: otherUserId);
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;
            var userId = Guid.NewGuid();
            Assert.That(userId, Is.Not.EqualTo(otherUserId));

            // Act
            var context = todo.Context = TodoTestHelpers.GetContext(userId: userId);
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
            var context = TodoTestHelpers.GetContext();
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
