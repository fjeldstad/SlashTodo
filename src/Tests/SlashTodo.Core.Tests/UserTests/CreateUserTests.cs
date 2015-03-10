using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Tests.Common;

namespace SlashTodo.Core.Tests.UserTests
{
    [TestFixture]
    public class CreateUserTests
    {
        [Test]
        public void CanNotCreateUserWithoutId()
        {
            Assert.Throws<ArgumentNullException>(() => User.Create(null, "teamId"));
            Assert.Throws<ArgumentNullException>(() => User.Create(string.Empty, "teamId"));
            Assert.Throws<ArgumentNullException>(() => User.Create(" ", "teamId"));
        }

        [Test]
        public void CanNotCreateUserWithoutTeamId()
        {
            Assert.Throws<ArgumentNullException>(() => User.Create("id", null));
            Assert.Throws<ArgumentNullException>(() => User.Create("id", string.Empty));
            Assert.Throws<ArgumentNullException>(() => User.Create("id", " "));
        }

        [Test]
        public void CanCreateUser()
        {
            // Arrange
            var id = "id";
            var teamId = "teamId";
            var before = DateTime.UtcNow;

            // Act
            var user = User.Create(id, teamId);

            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Id, Is.EqualTo(id));
            Assert.That(user.Version, Is.GreaterThanOrEqualTo(1));
            var @event = user.GetUncommittedEvents().First() as UserCreated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: 0);
        }

        [Test]
        public void UserIsActivatedWhenCreated()
        {
            // Arrange
            var id = "id";
            var teamId = "teamId";
            var before = DateTime.UtcNow;

            // Act
            var user = User.Create(id, teamId);

            // Assert
            var @event = user.GetUncommittedEvents().Single(x => x is UserActivated) as UserActivated;
            @event.AssertThatBasicEventDataIsCorrect(id, before);
        }
    }
}
