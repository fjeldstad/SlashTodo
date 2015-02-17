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
    public class FreeTests
    {
        [Test]
        public void FreeingAnAlreadyFreeTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();

            // Act
            todo.Free();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void FreeingARemovedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Free();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void CannotFreeATodoClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var todo = Common.GetTodoClaimedBySomeoneElse();

            // Act & assert
            Assert.Throws<TodoClaimedBySomeoneElseException>(() => todo.Free());
        }

        [Test]
        public void CanFreeATodoClaimedBySomeoneElseUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodoClaimedBySomeoneElse(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Free(force: true);

            // Assert
            var todoFreed = todo.GetUncommittedEvents().SingleOrDefault() as TodoFreed;
            Assert.IsNotNull(todoFreed);
            Assert.That(todoFreed.Id, Is.EqualTo(todo.Id));
            Assert.That(todoFreed.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoFreed.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoFreed.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void CanFreeATodoClaimedByMyselfWithoutUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            todo.Claim();
            todo.ClearUncommittedEvents();
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Free();

            // Assert
            var todoFreed = todo.GetUncommittedEvents().SingleOrDefault() as TodoFreed;
            Assert.IsNotNull(todoFreed);
            Assert.That(todoFreed.Id, Is.EqualTo(todo.Id));
            Assert.That(todoFreed.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoFreed.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoFreed.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }
    }
}
