using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using Nancy.Authentication.Forms;
using NUnit.Framework;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Queries;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class UserMapperTests
    {
        private Mock<IUserQuery> _userQueryMock;
        private Mock<IQueryTeamsById> _accountQueryMock;
        private UserMapper _userMapper;

        [SetUp]
        public void BeforeEachTest()
        {
            _userQueryMock = new Mock<IUserQuery>();
            _accountQueryMock = new Mock<IQueryTeamsById>();
            _userMapper = new UserMapper(_userQueryMock.Object, _accountQueryMock.Object);
        }

        [Test]
        public void ReturnsNullWhenUserDoesNotExist()
        {
            // Arrange
            _userQueryMock.Setup(x => x.ById(It.IsAny<Guid>())).Returns(Task.FromResult<UserDto>(null));

            // Act
            var userIdentity = _userMapper.GetUserFromIdentifier(Guid.NewGuid(), It.IsAny<NancyContext>());

            // Assert
            Assert.That(userIdentity, Is.Null);
        }

        [Test]
        public void ReturnsNullWhenAccountDoesNotExist()
        {
            // Arrange
            var userDto = GetUserDto();
            _userQueryMock.Setup(x => x.ById(It.IsAny<Guid>())).Returns(Task.FromResult(userDto));
            _accountQueryMock.Setup(x => x.Query(It.IsAny<Guid>())).Returns(Task.FromResult<TeamDto>(null));

            // Act
            var userIdentity = _userMapper.GetUserFromIdentifier(Guid.NewGuid(), It.IsAny<NancyContext>());

            // Assert
            Assert.That(userIdentity, Is.Null);
        }

        [Test]
        public void ReturnsSlackUserIdentityWhenUserExists()
        {
            // Arrange
            var accountDto = GetAccountDto();
            var userDto = GetUserDto();
            userDto.TeamId = accountDto.Id;
            _userQueryMock.Setup(x => x.ById(It.IsAny<Guid>())).Returns(Task.FromResult(userDto));
            _accountQueryMock.Setup(x => x.Query(It.IsAny<Guid>())).Returns(Task.FromResult(accountDto));

            // Act
            var userIdentity = _userMapper.GetUserFromIdentifier(Guid.NewGuid(), It.IsAny<NancyContext>()) as SlackUserIdentity;

            // Assert
            Assert.That(userIdentity, Is.Not.Null);
            Assert.That(userIdentity.Id, Is.EqualTo(userDto.Id));
            Assert.That(userIdentity.AccountId, Is.EqualTo(accountDto.Id));
            Assert.That(userIdentity.SlackUserId, Is.EqualTo(userDto.SlackUserId));
            Assert.That(userIdentity.SlackUserName, Is.EqualTo(userDto.Name));
            Assert.That(userIdentity.SlackApiAccessToken, Is.EqualTo(userDto.SlackApiAccessToken));
            Assert.That(userIdentity.SlackTeamId, Is.EqualTo(accountDto.SlackTeamId));
            Assert.That(userIdentity.SlackTeamName, Is.EqualTo(accountDto.Name));
            Assert.That(userIdentity.SlackTeamUrl, Is.EqualTo(accountDto.SlackUrl));
        }

        private static UserDto GetUserDto()
        {
            return new UserDto
            {
                Id = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                SlackUserId = "slackUserId",
                Name = "slackUserName",
                SlackApiAccessToken = "slackApiAccessToken",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = true
            };
        }

        private static TeamDto GetAccountDto()
        {
            return new TeamDto
            {
                Id = Guid.NewGuid(),
                SlackTeamId = "slackTeamId",
                Name = "slackTeamName",
                SlackUrl = new Uri("https://team.slack.com"),
                IncomingWebhookUrl = new Uri("https://api.slack.com/incoming-webhook"),
                SlashCommandToken = "slashCommandToken",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = true
            };
        }
    }
}
