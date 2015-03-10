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
    public class UpdateSlashCommandTokenTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void CanSetSlashCommandTokenToNullOrWhitespace(string token)
        {
            // Arrange
            var id = "teamId";
            var team = Team.Create(id);
            team.UpdateSlashCommandToken("slashCommandToken");
            team.ClearUncommittedEvents();
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            team.UpdateSlashCommandToken(token);

            // Assert
            var @event = team.GetUncommittedEvents().Single(x => x is TeamSlashCommandTokenUpdated) as TeamSlashCommandTokenUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlashCommandToken, Is.Null);
        }

        [Test]
        public void CanUpdateSlashCommandToken()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            team.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            var @event = team.GetUncommittedEvents().Single(x => x is TeamSlashCommandTokenUpdated) as TeamSlashCommandTokenUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlashCommandToken, Is.EqualTo(slashCommandToken));
        }

        [Test]
        public void UpdateSlashCommandTokenIsIdempotentOperation()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();

            // Act
            team.UpdateSlashCommandToken(slashCommandToken);
            team.UpdateSlashCommandToken(slashCommandToken);
            team.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            Assert.That(team.GetUncommittedEvents().Where(x => x is TeamSlashCommandTokenUpdated).ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateSlashCommandTokenMultipleTimes()
        {
            // Arrange
            var slashCommandTokens = new string[]
            {
                "slashCommandToken1",
                "slashCommandToken2",
                "slashCommandToken3"
            };
            var id = "teamId";
            var team = Team.Create(id);
            team.ClearUncommittedEvents();
            var before = DateTime.UtcNow;

            // Act
            foreach (var slashCommandToken in slashCommandTokens)
            {
                team.UpdateSlashCommandToken(slashCommandToken);
            }

            // Assert
            var events = team.GetUncommittedEvents().Where(x => x is TeamSlashCommandTokenUpdated).Cast<TeamSlashCommandTokenUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlashCommandToken).SequenceEqual(slashCommandTokens));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before);
            }
        }
    }
}
