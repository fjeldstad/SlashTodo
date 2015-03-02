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
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Free();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoFreed;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void FreeTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
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
            var otherUserContext = TestHelpers.GetContext(userId: "otherUserId");
            var todo = Todo.Add(id, otherUserContext, "text");
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act & assert
            todo.Context = TestHelpers.GetContext();
            TestHelpers.AssertThrows<TodoClaimedBySomeoneElseException>(
                () => todo.Free(),
                ex => ex.ClaimedBy == otherUserContext.UserId);
        }

        [Test]
        public void CanFreeTodoThatIsClaimedBySomeoneElseWhenUsingForce()
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
            todo.Free(force: true);

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoFreed;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }
    }
}
