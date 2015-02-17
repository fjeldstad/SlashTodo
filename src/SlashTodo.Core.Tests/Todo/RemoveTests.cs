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
    public class RemoveTests
    {
        [Test]
        public void CanRemoveUntickedUnclaimedTodo()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Remove();

            // Assert
            var todoRemoved = todo.GetUncommittedEvents().SingleOrDefault() as TodoRemoved;
            Assert.IsNotNull(todoRemoved);
            Assert.That(todoRemoved.Id, Is.EqualTo(todo.Id));
            Assert.That(todoRemoved.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoRemoved.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoRemoved.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void CanRemoveTickedTodo()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            todo.Tick();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Remove();

            // Assert
            var todoRemoved = todo.GetUncommittedEvents().SingleOrDefault() as TodoRemoved;
            Assert.IsNotNull(todoRemoved);
            Assert.That(todoRemoved.Id, Is.EqualTo(todo.Id));
            Assert.That(todoRemoved.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoRemoved.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoRemoved.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void RemovingAnAlreadyRemovedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Remove();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void CannotRemoveATodoClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var todo = Common.GetTodoClaimedBySomeoneElse();

            // Act & assert
            Assert.Throws<TodoClaimedBySomeoneElseException>(() => todo.Remove());
        }

        [Test]
        public void CanRemoveATodoClaimedBySomeoneElseUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodoClaimedBySomeoneElse(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Remove(force: true);

            // Assert
            var todoRemoved = todo.GetUncommittedEvents().SingleOrDefault() as TodoRemoved;
            Assert.IsNotNull(todoRemoved);
            Assert.That(todoRemoved.Id, Is.EqualTo(todo.Id));
            Assert.That(todoRemoved.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoRemoved.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoRemoved.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void CanRemoveATodoClaimedByMyselfWithoutUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Remove();

            // Assert
            var todoRemoved = todo.GetUncommittedEvents().SingleOrDefault() as TodoRemoved;
            Assert.IsNotNull(todoRemoved);
            Assert.That(todoRemoved.Id, Is.EqualTo(todo.Id));
            Assert.That(todoRemoved.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoRemoved.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoRemoved.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }
    }
}
