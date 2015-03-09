using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Tests.Common;

namespace SlashTodo.Core.Tests.UserTests
{
    [TestFixture]
    public class UpdateSlackApiAccessTokenTests
    {
        [Test]
        public void CanNotUpdateSlackApiAccessTokenWithEmptyValue()
        {
            // Arrange
            var user = User.Create(Guid.NewGuid(), Guid.NewGuid(), "slackUserId");
            
            // Act
            Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateSlackApiAccessToken(string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateSlackApiAccessToken(" "));
        }

        [Test]
        public void CanSetSlackApiAccessTokenToNull()
        {
            // Arrange
            var user = User.Create(Guid.NewGuid(), Guid.NewGuid(), "slackUserId");

            // Act
            Assert.DoesNotThrow(() => user.UpdateSlackApiAccessToken(null));
        }

        [Test]
        public void CanUpdateSlackApiAccessToken()
        {
            // Arrange
            var slackApiAccessToken = "slackApiAccessToken";
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            user.UpdateSlackApiAccessToken(slackApiAccessToken);

            // Assert
            var @event = user.GetUncommittedEvents().Single() as UserSlackApiAccessTokenUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlackApiAccessToken, Is.EqualTo(slackApiAccessToken));
        }

        [Test]
        public void UpdateSlackApiAccessTokenIsIdempotentOperation()
        {
            // Arrange
            var slackApiAccessToken = "slackApiAccessToken";
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.ClearUncommittedEvents();

            // Act
            user.UpdateSlackApiAccessToken(slackApiAccessToken);
            user.UpdateSlackApiAccessToken(slackApiAccessToken);
            user.UpdateSlackApiAccessToken(slackApiAccessToken);

            // Assert
            Assert.That(user.GetUncommittedEvents().ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateSlackApiAccessTokenMultipleTimes()
        {
            // Arrange
            var slackApiAccessTokens = new []
            {
                "slackApiAccessToken1",
                "slackApiAccessToken2",
                "slackApiAccessToken3"
            };
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var slackApiAccessToken in slackApiAccessTokens)
            {
                user.UpdateSlackApiAccessToken(slackApiAccessToken);
            }

            // Assert
            var events = user.GetUncommittedEvents().Cast<UserSlackApiAccessTokenUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlackApiAccessToken).SequenceEqual(slackApiAccessTokens));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }

        [Test]
        public void SettingSlackApiAccessTokenToNullRaisesCorrectEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.UpdateSlackApiAccessToken("originalSlackApiToken");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            user.UpdateSlackApiAccessToken(null);

            // Assert
            var @event = user.GetUncommittedEvents().Single() as UserSlackApiAccessTokenUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlackApiAccessToken, Is.Null);
        }

        [Test]
        public void SettingSlackApiAccessTokenToNullIsIdempotentOperation()
        {
            // Arrange
            var slackApiAccessToken = "originalSlackApiToken";
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.UpdateSlackApiAccessToken(slackApiAccessToken);
            user.ClearUncommittedEvents();

            // Act
            user.UpdateSlackApiAccessToken(null);
            user.UpdateSlackApiAccessToken(null);
            user.UpdateSlackApiAccessToken(null);

            // Assert
            Assert.That(user.GetUncommittedEvents().ToArray(), Has.Length.EqualTo(1));
        }
    }
}
