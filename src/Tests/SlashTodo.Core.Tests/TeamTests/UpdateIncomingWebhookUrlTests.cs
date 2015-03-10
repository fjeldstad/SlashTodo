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
    public class UpdateIncomingWebhookUrlTests
    {
        [Test]
        public void CanNotConfigureTeamWithInvalidIncomingWebhookUrl()
        {
            // Arrange
            var team = Team.Create("teamId");

            // Act
            Assert.Throws<ArgumentException>(() => team.UpdateIncomingWebhookUrl(new Uri("about:blank")));
        }

        [Test]
        public void CanSetIncomingWebhookUrlToNull()
        {
            // Arrange
            var id = "teamId";
            var team = Team.Create(id);
            team.UpdateIncomingWebhookUrl(new Uri("https://api.slack.com/incoming-webhook"));
            team.ClearUncommittedEvents();
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            team.UpdateIncomingWebhookUrl(null);

            // Assert
            var @event = team.GetUncommittedEvents().Single(x => x is TeamIncomingWebhookUpdated) as TeamIncomingWebhookUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.IncomingWebhookUrl, Is.Null);
        }

        [Test]
        public void CanUpdateIncomingWebhookUrl()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            team.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            var @event = team.GetUncommittedEvents().Single(x => x is TeamIncomingWebhookUpdated) as TeamIncomingWebhookUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.IncomingWebhookUrl, Is.EqualTo(incomingWebhookUrl));
        }

        [Test]
        public void UpdateIncomingWebhookUrlIsIdempotentOperation()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();

            // Act
            team.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            team.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            team.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            Assert.That(team.GetUncommittedEvents().Where(x => x is TeamIncomingWebhookUpdated).ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateIncomingWebhookUrlMultipleTimes()
        {
            // Arrange
            var incomingWebhookUrls = new Uri[]
            {
                new Uri("https://test.slashtodo.com/1"),
                new Uri("https://test.slashtodo.com/2"),
                new Uri("https://test.slashtodo.com/3")
            };
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var before = DateTime.UtcNow;

            // Act
            foreach (var incomingWebhookUrl in incomingWebhookUrls)
            {
                team.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            }

            // Assert
            var events = team.GetUncommittedEvents().Where(x => x is TeamIncomingWebhookUpdated).Cast<TeamIncomingWebhookUpdated>().ToArray();
            Assert.That(events.Select(x => x.IncomingWebhookUrl).SequenceEqual(incomingWebhookUrls));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before);
            }
        }
    }
}
