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
    public class CreateTeamTests
    {
        [Test]
        public void CanNotCreateTeamWithoutId()
        {
            Assert.Throws<ArgumentNullException>(() => Team.Create(null));
            Assert.Throws<ArgumentNullException>(() => Team.Create(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Team.Create(" "));
        }

        [Test]
        public void CanCreateTeam()
        {
            // Arrange
            const string id = "teamId";
            var before = DateTime.UtcNow;

            // Act
            var team = Team.Create(id);

            // Assert
            Assert.That(team, Is.Not.Null);
            Assert.That(team.Id, Is.EqualTo(id));
            Assert.That(team.Version, Is.EqualTo(1));
            var @event = team.GetUncommittedEvents().Single() as TeamCreated;
            @event.AssertThatBasicEventDataIsCorrect(id, before, expectedOriginalVersion: 0);
        }
    }
}
