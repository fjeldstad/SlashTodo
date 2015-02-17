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
    public class ClaimTests
    {
        [Test]
        public void CanClaimUnclaimedTodo()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodo(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Claim();

            // Assert
            var todoClaimed = todo.GetUncommittedEvents().SingleOrDefault() as TodoClaimed;
            Assert.IsNotNull(todoClaimed);
            Assert.That(todoClaimed.Id, Is.EqualTo(todo.Id));
            Assert.That(todoClaimed.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoClaimed.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoClaimed.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }

        [Test]
        public void ClaimingATodoAdditionalTimesDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Claim();
            todo.ClearUncommittedEvents();

            // Act
            todo.Claim();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void ClaimingARemovedTodoDoesNothing()
        {
            // Arrange
            var todo = Common.GetTodo();
            todo.Remove();
            todo.ClearUncommittedEvents();

            // Act
            todo.Claim();

            // Assert
            Assert.IsEmpty(todo.GetUncommittedEvents());
        }

        [Test]
        public void CannotClaimATodoClaimedBySomeoneElseWithoutUsingForce()
        {
            // Arrange
            var todo = Common.GetTodoClaimedBySomeoneElse();

            // Act & assert
            Assert.Throws<TodoClaimedBySomeoneElseException>(() => todo.Claim());
        }

        [Test]
        public void CanClaimATodoClaimedBySomeoneElseUsingForce()
        {
            // Arrange
            var context = Common.GetContext();
            var todo = Common.GetTodoClaimedBySomeoneElse(context);
            var originalVersion = todo.Version;
            var before = DateTime.UtcNow;

            // Act
            todo.Claim(force: true);

            // Assert
            var todoClaimed = todo.GetUncommittedEvents().SingleOrDefault() as TodoClaimed;
            Assert.IsNotNull(todoClaimed);
            Assert.That(todoClaimed.Id, Is.EqualTo(todo.Id));
            Assert.That(todoClaimed.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoClaimed.OriginalVersion, Is.EqualTo(originalVersion));
            Assert.That(todoClaimed.Timestamp, Is.InRange(before, DateTime.UtcNow));
        }
    }
}
