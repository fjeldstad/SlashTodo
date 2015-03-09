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
    public class UpdateSlackUserNameTests
    {
        [Test]
        public void CanNotUpdateSlackUserNameWithMissingValue()
        {
            // Arrange
            var user = User.Create(Guid.NewGuid(), Guid.NewGuid(), "slackUserId");

            // Act
            Assert.Throws<ArgumentNullException>(() => user.UpdateSlackUserName(null));
            Assert.Throws<ArgumentNullException>(() => user.UpdateSlackUserName(string.Empty));
            Assert.Throws<ArgumentNullException>(() => user.UpdateSlackUserName(" "));
        }

        [Test]
        public void CanUpdateSlackUserName()
        {
            // Arrange
            var slackUserName = " slackUserName ";
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            user.UpdateSlackUserName(slackUserName);

            // Assert
            var @event = user.GetUncommittedEvents().Single() as UserSlackUserNameUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.SlackUserName, Is.EqualTo(slackUserName.Trim()));
        }

        [Test]
        public void UpdateSlackUserNameIsIdempotentOperation()
        {
            // Arrange
            var slackUserName = "slackUserName";
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.ClearUncommittedEvents();

            // Act
            user.UpdateSlackUserName(slackUserName);
            user.UpdateSlackUserName(slackUserName);
            user.UpdateSlackUserName(slackUserName);

            // Assert
            Assert.That(user.GetUncommittedEvents().ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CanUpdateSlackUserNameMultipleTimes()
        {
            // Arrange
            var slackUserNames = new string[]
            {
                "slackUserName1",
                "slackUserName2",
                "slackUserName3"
            };
            var id = Guid.NewGuid();
            var user = User.Create(id, Guid.NewGuid(), "slackUserId");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var slackUserName in slackUserNames)
            {
                user.UpdateSlackUserName(slackUserName);
            }

            // Assert
            var events = user.GetUncommittedEvents().Cast<UserSlackUserNameUpdated>().ToArray();
            Assert.That(events.Select(x => x.SlackUserName).SequenceEqual(slackUserNames));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }
    }
}
