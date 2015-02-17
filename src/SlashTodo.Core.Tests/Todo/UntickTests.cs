using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Tests.Todo;

namespace SlashTodo.Core.Tests
{
    [TestFixture]
    public class UntickTests
    {
        [Test]
        public void CanUntickTickedTodo()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            todo.Tick();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Untick();

            // Assert
            var todoUnticked = todo.GetUncommittedEvents().SingleOrDefault() as TodoUnticked;
            Assert.IsNotNull(todoUnticked);
            Assert.That(todoUnticked.Id, Is.EqualTo(todo.Id));
            Assert.That(todoUnticked.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoUnticked.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoUnticked.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void UntickingAnAlreadyUntickedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();

            // Act
            todo.Untick();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void UntickingARemovedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Untick();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }
    }
}
