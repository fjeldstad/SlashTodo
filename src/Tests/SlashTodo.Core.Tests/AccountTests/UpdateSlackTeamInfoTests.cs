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
    public class UpdateSlackTeamInfoTests
    {
        [Test]
        public void CanNotConfigureAccountWithEmptySlackTeamInfo()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), "teamId");

            // Act
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamInfo(null, It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamInfo(string.Empty, It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamInfo(" ", It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlackTeamInfo(It.IsAny<string>(), null));
        }

        [Test]
        public void CanUpdateSlackTeamInfo()
        {
            // Arrange
            var slackTeamName = " slackTeamName ";
            var slackTeamUrl = new Uri("https://team.slack.com");
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            account.UpdateSlackTeamInfo(slackTeamName, slackTeamUrl);

            // Assert
            var @event = account.GetUncommittedEvents().Single() as AccountSlackTeamInfoUpdated;
            @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlackTeamName, Is.EqualTo(slackTeamName.Trim()));
            Assert.That(@event.SlackTeamUrl, Is.EqualTo(slackTeamUrl));
        }

        [Test]
        public void UpdateSlackTeamInfoIsIdempotentOperation()
        {
            // Arrange
            var slackTeamName = "slackTeamName";
            var slackTeamUrl = new Uri("https://team.slack.com");
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();

            // Act
            account.UpdateSlackTeamInfo(slackTeamName, slackTeamUrl);
            account.UpdateSlackTeamInfo(slackTeamName, slackTeamUrl);
            account.UpdateSlackTeamInfo(slackTeamName, slackTeamUrl);

            // Assert
            Assert.That(account.GetUncommittedEvents().ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateSlackTeamInfoMultipleTimes()
        {
            // Arrange
            var slackTeamInfo = new Tuple<string, Uri>[]
            {
                new Tuple<string, Uri>("slackTeamName1", new Uri("https://team1.slack.com")), 
                new Tuple<string, Uri>("slackTeamName2", new Uri("https://team2.slack.com")), 
                new Tuple<string, Uri>("slackTeamName3", new Uri("https://team3.slack.com"))
            };
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var info in slackTeamInfo)
            {
                account.UpdateSlackTeamInfo(info.Item1, info.Item2);
            }

            // Assert
            var events = account.GetUncommittedEvents().Cast<AccountSlackTeamInfoUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlackTeamName).SequenceEqual(slackTeamInfo.Select(x => x.Item1)));
            Assert.That(events.Select(x => x.SlackTeamUrl).SequenceEqual(slackTeamInfo.Select(x => x.Item2)));
            foreach (var @event in events)
            {
                @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }
    }
}
