using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Tests.Common;

namespace SlashTodo.Core.Tests.TeamTests
{
    [TestFixture]
    public class UpdateInfoTests
    {
        [Test]
        public void CanNotConfigureTeamWithEmptySlackTeamInfo()
        {
            // Arrange
            var team = Team.Create("teamId");

            // Act
            Assert.Throws<ArgumentNullException>(() => team.UpdateInfo(null, It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(() => team.UpdateInfo(string.Empty, It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(() => team.UpdateInfo(" ", It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(() => team.UpdateInfo(It.IsAny<string>(), null));
        }

        [Test]
        public void CanUpdateInfo()
        {
            // Arrange
            var name = " slackTeamName ";
            var slackUrl = new Uri("https://team.slack.com");
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            team.UpdateInfo(name, slackUrl);

            // Assert
            var @event = team.GetUncommittedEvents().Single() as TeamInfoUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.Name, Is.EqualTo(name.Trim()));
            Assert.That(@event.SlackUrl, Is.EqualTo(slackUrl));
        }

        [Test]
        public void UpdateInfoIsIdempotentOperation()
        {
            // Arrange
            var name = "slackTeamName";
            var slackUrl = new Uri("https://team.slack.com");
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();

            // Act
            team.UpdateInfo(name, slackUrl);
            team.UpdateInfo(name, slackUrl);
            team.UpdateInfo(name, slackUrl);

            // Assert
            Assert.That(team.GetUncommittedEvents().ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateInfoMultipleTimes()
        {
            // Arrange
            var info = new Tuple<string, Uri>[]
            {
                new Tuple<string, Uri>("slackTeamName1", new Uri("https://team1.slack.com")), 
                new Tuple<string, Uri>("slackTeamName2", new Uri("https://team2.slack.com")), 
                new Tuple<string, Uri>("slackTeamName3", new Uri("https://team3.slack.com"))
            };
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var i in info)
            {
                team.UpdateInfo(i.Item1, i.Item2);
            }

            // Assert
            var events = team.GetUncommittedEvents().Cast<TeamInfoUpdated>().ToArray();
            Assert.That(events.Select(x => x.Name).SequenceEqual(info.Select(x => x.Item1)));
            Assert.That(events.Select(x => x.SlackUrl).SequenceEqual(info.Select(x => x.Item2)));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }
    }
}
