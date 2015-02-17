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
    public class TickTests
    {
        [Test]
        public void CanTickUnclaimedTodo()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Tick();

            // Assert
            var todoTicked = todo.GetUncommittedEvents().SingleOrDefault() as TodoTicked;
            Assert.IsNotNull(todoTicked);
            Assert.That(todoTicked.Id, Is.EqualTo(todo.Id));
            Assert.That(todoTicked.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoTicked.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoTicked.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void TickingAnAlreadyTickedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Tick();
            todo.ClearUncommittedEvents();

            // Act
            todo.Tick();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void TickingARemovedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Tick();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void CannotTickATodoClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var todo = Common.GetTodoClaimedBySomeoneElse();

            // Act & assert
            Assert.Throws<TodoClaimedBySomeoneElseException>(() => todo.Tick());
        }

        [Test]
        public void CanTickATodoClaimedBySomeoneElseUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodoClaimedBySomeoneElse(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Tick(force: true);

            // Assert
            var todoTicked = todo.GetUncommittedEvents().SingleOrDefault() as TodoTicked;
            Assert.IsNotNull(todoTicked);
            Assert.That(todoTicked.Id, Is.EqualTo(todo.Id));
            Assert.That(todoTicked.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoTicked.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoTicked.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void CanTickATodoClaimedByMyselfWithoutUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Tick();

            // Assert
            var todoTicked = todo.GetUncommittedEvents().SingleOrDefault() as TodoTicked;
            Assert.IsNotNull(todoTicked);
            Assert.That(todoTicked.Id, Is.EqualTo(todo.Id));
            Assert.That(todoTicked.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoTicked.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoTicked.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }
    }
}
