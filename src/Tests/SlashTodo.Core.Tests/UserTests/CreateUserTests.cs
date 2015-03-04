using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.UserTests
{
    [TestFixture]
    public class CreateUserTests
    {
        [Test]
        public void CanNotCreateUserWithoutSlackUserId()
        {
            Assert.Throws<ArgumentNullException>(() => User.Create(It.IsAny<Guid>(), It.IsAny<Guid>(), null));
            Assert.Throws<ArgumentNullException>(() => User.Create(It.IsAny<Guid>(), It.IsAny<Guid>(), string.Empty));
            Assert.Throws<ArgumentNullException>(() => User.Create(It.IsAny<Guid>(), It.IsAny<Guid>(), " "));
        }

        [Test]
        public void CanCreateUser()
        {
            // Arrange
            var id = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            const string slackUserId = "userId";
            var before = DateTime.UtcNow;

            // Act
            var user = User.Create(id, accountId, slackUserId);

            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Id, Is.EqualTo(id));
            Assert.That(user.Version, Is.GreaterThanOrEqualTo(1));
            var @event = user.GetUncommittedEvents().First() as UserCreated;
            @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: 0);
            Assert.That(@event.SlackUserId, Is.EqualTo(slackUserId));
        }

        [Test]
        public void UserIsActivatedWhenCreated()
        {
            // Arrange
            var id = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            const string slackUserId = "userId";
            var before = DateTime.UtcNow;

            // Act
            var user = User.Create(id, accountId, slackUserId);

            // Assert
            var @event = user.GetUncommittedEvents().Single(x => x is UserActivated) as UserActivated;
            @event.AssertThatBasicDataIsCorrect(id, before);
        }
    }
}
