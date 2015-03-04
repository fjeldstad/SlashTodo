using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.AccountTests
{
    [TestFixture]
    public class CreateAccountTests
    {
        [Test]
        public void CanNotCreateAccountWithoutTeamId()
        {
            Assert.Throws<ArgumentNullException>(() => Account.Create(It.IsAny<Guid>(), null));
            Assert.Throws<ArgumentNullException>(() => Account.Create(It.IsAny<Guid>(), string.Empty));
            Assert.Throws<ArgumentNullException>(() => Account.Create(It.IsAny<Guid>(), " "));
        }

        [Test]
        public void CanCreateAccount()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string teamId = "teamId";
            var before = DateTime.UtcNow;

            // Act
            var account = Account.Create(id, teamId);

            // Assert
            Assert.That(account, Is.Not.Null);
            Assert.That(account.Id, Is.EqualTo(id));
            Assert.That(account.Version, Is.EqualTo(1));
            var @event = account.GetUncommittedEvents().Single() as AccountCreated;
            @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: 0);
            Assert.That(@event.SlackTeamId, Is.EqualTo(teamId));
        }
    }
}
