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
    public class UntickTodoTests
    {
        [Test]
        public void CanUntickTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.Tick();
            todo.ClearUncommittedEvents();
            var before = DateTime.UtcNow;
            var originalVersion = todo.Version;

            // Act
            todo.Untick();

            // Assert
            var @event = todo.GetUncommittedEvents().Single() as TodoUnticked;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: originalVersion);
        }

        [Test]
        public void UntickTodoIsIdempotentOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
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
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var todo = Todo.Add(id, context, "text");
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Untick();

            // Assert
            Assert.That(todo.GetUncommittedEvents(), Is.Empty);
        }
    }
}
