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
    public class UpdateSlashCommandTokenTests
    {
        [Test]
        public void CanNotConfigureAccountWithEmptySlashCommandToken()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), "teamId");

            // Act
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlashCommandToken(null));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlashCommandToken(string.Empty));
            Assert.Throws<ArgumentNullException>(() => account.UpdateSlashCommandToken(" "));
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
            var @event = account.GetUncommittedEvents().Single() as AccountSlashCommandTokenUpdated;
            @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlashCommandToken, Is.EqualTo(slashCommandToken));
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
            var events = account.GetUncommittedEvents().Cast<AccountSlashCommandTokenUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlashCommandToken).SequenceEqual(slashCommandTokens));
            foreach (var @event in events)
            {
                @event.AssertThatBasicDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }

        [Test]
        public void SettingSlashCommandTokenActivatesAccountIfIncomingWebhookUrlHasBeenConfigured()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.UpdateIncomingWebhookUrl(new Uri("https://test.slashtodo.com"));
            account.ClearUncommittedEvents();

            // Act
            account.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            Assert.That(account.GetUncommittedEvents().Any(x => x is AccountActivated));
        }
    }
}
