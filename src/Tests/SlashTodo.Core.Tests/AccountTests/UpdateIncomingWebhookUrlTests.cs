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
    public class UpdateIncomingWebhookUrlTests
    {
        [Test]
        public void CanNotConfigureAccountWithInvalidIncomingWebhookUrl()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), "teamId");

            // Act
            Assert.Throws<ArgumentException>(() => account.UpdateIncomingWebhookUrl(new Uri("about:blank")));
        }

        [Test]
        public void CanSetIncomingWebhookUrlToNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.UpdateIncomingWebhookUrl(new Uri("https://api.slack.com/incoming-webhook"));
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            account.UpdateIncomingWebhookUrl(null);

            // Assert
            var @event = account.GetUncommittedEvents().Single(x => x is AccountIncomingWebhookUpdated) as AccountIncomingWebhookUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.IncomingWebhookUrl, Is.Null);
        }

        [Test]
        public void CanUpdateIncomingWebhookUrl()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            var @event = account.GetUncommittedEvents().Single(x => x is AccountIncomingWebhookUpdated) as AccountIncomingWebhookUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.IncomingWebhookUrl, Is.EqualTo(incomingWebhookUrl));
        }

        [Test]
        public void UpdateIncomingWebhookUrlIsIdempotentOperation()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://test.slashtodo.com");
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();

            // Act
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);

            // Assert
            Assert.That(account.GetUncommittedEvents().Where(x => x is AccountIncomingWebhookUpdated).ToArray(), Has.Length.EqualTo(1));
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
            var id = Guid.NewGuid();
            var account = Account.Create(id, "teamId");
            account.ClearUncommittedEvents();
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var incomingWebhookUrl in incomingWebhookUrls)
            {
                account.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            }

            // Assert
            var events = account.GetUncommittedEvents().Where(x => x is AccountIncomingWebhookUpdated).Cast<AccountIncomingWebhookUpdated>().ToArray();
            Assert.That(events.Select(x => x.IncomingWebhookUrl).SequenceEqual(incomingWebhookUrls));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before);
            }
        }
    }
}
