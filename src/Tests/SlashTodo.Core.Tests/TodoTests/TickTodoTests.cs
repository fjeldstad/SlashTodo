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
    public class TickTodoTests
    {
        [Test]
        public void CanTickTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Tick();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoTicked;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void TickTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
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
            var todo = Todo.Add(id, context, "text");
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
            var otherUserContext = TodoTestHelpers.GetContext(userId: "otherUserId");
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act & assert
            todo.Context = TodoTestHelpers.GetContext();
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Tick(),
                ex => ex.ClaimedBy == otherUserContext.UserId);
        }

        [Test]
        public void CanTickTodoThatIsClaimedBySomeoneElseWhenUsingForce()
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
            todo.Tick(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoTicked;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void CanTickTodoThatIsClaimedBySameUserWithoutUsingForce()
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
            todo.Tick();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoTicked;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }
    }
}
