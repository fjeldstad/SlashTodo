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
            var user = User.Create("id", "teamId");

            // Act
            Assert.Throws<ArgumentNullException>(() => user.UpdateName(null));
            Assert.Throws<ArgumentNullException>(() => user.UpdateName(string.Empty));
            Assert.Throws<ArgumentNullException>(() => user.UpdateName(" "));
        }

        [Test]
        public void CanUpdateSlackUserName()
        {
            // Arrange
            var slackUserName = " slackUserName ";
            var id = "id";
            var user = User.Create(id, "teamId");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            user.UpdateName(slackUserName);

            // Assert
            var @event = user.GetUncommittedEvents().Single() as UserNameUpdated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion);
            Assert.That(@event.Name, Is.EqualTo(slackUserName.Trim()));
        }

        [Test]
        public void UpdateSlackUserNameIsIdempotentOperation()
        {
            // Arrange
            var slackUserName = "slackUserName";
            var id = "id";
            var user = User.Create(id, "teamId");
            user.ClearUncommittedEvents();

            // Act
            user.UpdateName(slackUserName);
            user.UpdateName(slackUserName);
            user.UpdateName(slackUserName);

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
            var id = "id";
            var user = User.Create(id, "teamId");
            user.ClearUncommittedEvents();
            var originalVersion = user.Version;
            var before = DateTime.UtcNow;

            // Act
            foreach (var slackUserName in slackUserNames)
            {
                user.UpdateName(slackUserName);
            }

            // Assert
            var events = user.GetUncommittedEvents().Cast<UserNameUpdated>().ToArray();
            Assert.That(events.Select(x => x.Name).SequenceEqual(slackUserNames));
            foreach (var @event in events)
            {
                @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: originalVersion++);
            }
        }
    }
}
