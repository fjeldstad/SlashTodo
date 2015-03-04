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
    public class UpdateSlackTeamNameTests
    {
        [Test]
        public void CanNotConfigureAccountWithEmptySlackTeamName()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), "teamId");

            // Act
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamName(null));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamName(string.Empty));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamName(" "));
        }

        [Test]
        public void CanUpdateSlackTeamName()
        {
            // Arrange
            var slackTeamName = " slackTeamName ";
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            account.UpdateSlackTeamName(slackTeamName);

            // Assert
            var @event = account.GetUncommittedEvents().Single() as AccountSlackTeamNameUpdated;
            @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlackTeamName, Is.EqualTo(slackTeamName.Trim()));
        }

        [Test]
        public void UpdateSlackTeamNameIsIdempotentOperation()
        {
            // Arrange
            var slackTeamName = "slackTeamName";
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();

            // Act
            account.UpdateSlackTeamName(slackTeamName);
            account.UpdateSlackTeamName(slackTeamName);
            account.UpdateSlackTeamName(slackTeamName);

            // Assert
            Assert.That(account.GetUncommittedEvents().ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateSlackTeamNameMultipleTimes()
        {
            // Arrange
            var slackTeamNames = new string[]
            {
                "slackTeamName1",
                "slackTeamName2",
                "slackTeamName3"
            };
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var slackTeamName in slackTeamNames)
            {
                account.UpdateSlackTeamName(slackTeamName);
            }

            // Assert
            var events = account.GetUncommittedEvents().Cast<AccountSlackTeamNameUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlackTeamName).SequenceEqual(slackTeamNames));
            foreach (var @event in events)
            {
                @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }
    }
}
