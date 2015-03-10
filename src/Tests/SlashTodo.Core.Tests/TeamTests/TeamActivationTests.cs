using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.TeamTests
{
    [TestFixture]
    public class TeamActivationTests
    {
        [Test]
        public void TeamIsActivatedWhenAllRequiredConfigurationHasBeenPerformed()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var before = DateTime.UtcNow;

            // Act
            team.UpdateSlashCommandToken(slashCommandToken);
            team.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            Assert.That(team.GetUncommittedEvents()
                .Where(x => x is TeamActivated)
                .Cast<TeamActivated>()
                .SingleOrDefault(x =>
                    x.Id == id &&
                    x.Timestamp >= before &&
                    x.Timestamp <= DateTime.UtcNow), Is.Not.Null);
        }

        [Test]
        public void TeamIsNotActivatedWhenIncomingWebhookUrlIsMissing()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();

            // Act
            team.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            Assert.That(team.GetUncommittedEvents().All(x => !(x is TeamActivated)));
        }

        [Test]
        public void TeamIsNotActivatedWhenSlashCommandTokenIsMissing()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();

            // Act
            team.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            Assert.That(team.GetUncommittedEvents().All(x => !(x is TeamActivated)));
        }

        [Test]
        public void TeamIsDeactivatedWhenSlashCommandTokenIsSetToNull()
        {
            // Arrange
            var id = "teamId";
            var team = Team.Create(id);
            team.UpdateSlashCommandToken("slashCommandToken");
            team.UpdateIncomingWebhookUrl(new Uri("https://api.slack.com/incoming-webhook"));
            team.ClearUncommittedEvents();
            Assert.That(team.IsActive);
            var before = DateTime.UtcNow;

            // Act
            team.UpdateSlashCommandToken(null);

            Assert.That(team.GetUncommittedEvents()
                .Where(x => x is TeamDeactivated)
                .Cast<TeamDeactivated>()
                .SingleOrDefault(x =>
                    x.Id == id &&
                    x.Timestamp >= before &&
                    x.Timestamp <= DateTime.UtcNow), Is.Not.Null);
        }

        [Test]
        public void TeamIsDeactivatedWhenIncomingWebhookUrlIsSetToNull()
        {
            // Arrange
            var id = "teamId";
            var team = Team.Create(id);
            team.UpdateSlashCommandToken("slashCommandToken");
            team.UpdateIncomingWebhookUrl(new Uri("https://api.slack.com/incoming-webhook"));
            team.ClearUncommittedEvents();
            Assert.That(team.IsActive);
            var before = DateTime.UtcNow;

            // Act
            team.UpdateIncomingWebhookUrl(null);

            Assert.That(team.GetUncommittedEvents()
                .Where(x => x is TeamDeactivated)
                .Cast<TeamDeactivated>()
                .SingleOrDefault(x =>
                    x.Id == id &&
                    x.Timestamp >= before &&
                    x.Timestamp <= DateTime.UtcNow), Is.Not.Null);
        }
    }
}
