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
    public class AccountActivationTests
    {
        [Test]
        public void AccountIsActivatedWhenAllRequiredConfigurationHasBeenPerformed()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var before = DateTime.UtcNow;

            // Act
            account.UpdateSlashCommandToken(slashCommandToken);
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            Assert.That(account.GetUncommittedEvents()
                .Where(x => x is AccountActivated)
                .Cast<AccountActivated>()
                .SingleOrDefault(x =>
                    x.Id == id &&
                    x.Timestamp >= before &&
                    x.Timestamp <= DateTime.UtcNow), Is.Not.Null);
        }

        [Test]
        public void AccountIsNotActivatedWhenIncomingWebhookUrlIsMissing()
        {
            // Arrange
            var slashCommandToken = "slashCommandToken";
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();

            // Act
            account.UpdateSlashCommandToken(slashCommandToken);

            // Assert
            Assert.That(account.GetUncommittedEvents().All(x => !(x is AccountActivated)));
        }

        [Test]
        public void AccountIsNotActivatedWhenSlashCommandTokenIsMissing()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();

            // Act
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            Assert.That(account.GetUncommittedEvents().All(x => !(x is AccountActivated)));
        }
    }
}
