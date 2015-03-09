using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Tests.Common;

namespace SlashTodo.Core.Tests.AccountTests
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
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.UpdateSlashCommandToken("slashCommandToken");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            account.UpdateSlashCommandToken(token);

            // Assert
            var @event = account.GetUncommittedEvents().Single(x => x is AccountSlashCommandTokenUpdated) as AccountSlashCommandTokenUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlashCommandToken, Is.Null);
        }

        [Test]
        public void CanUpdateSlashCommandToken()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            account.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            var @event = account.GetUncommittedEvents().Single(x => x is AccountSlashCommandTokenUpdated) as AccountSlashCommandTokenUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlashCommandToken, Is.EqualTo(slashCommandToken));
        }

        [Test]
        public void UpdateSlashCommandTokenIsIdempotentOperation()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();

            // Act
            account.UpdateSlashCommandToken(slashCommandToken);
            account.UpdateSlashCommandToken(slashCommandToken);
            account.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            Assert.That(account.GetUncommittedEvents().Where(x => x is AccountSlashCommandTokenUpdated).ToArray(), Has.Length.EqualTo(1));
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
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var slashCommandToken in slashCommandTokens)
            {
                account.UpdateSlashCommandToken(slashCommandToken);
            }

            // Assert
            var events = account.GetUncommittedEvents().Where(x => x is AccountSlashCommandTokenUpdated).Cast<AccountSlashCommandTokenUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlashCommandToken).SequenceEqual(slashCommandTokens));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before);
            }
        }
    }
}
