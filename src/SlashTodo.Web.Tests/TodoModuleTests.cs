using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using Nancy.Responses;
using Nancy.Testing;
using Newtonsoft.Json;
using NUnit.Framework;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Tests.Common;
using SlashTodo.Web.Api;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class TodoModuleTests
    {
        private Mock<ISlashCommandErrorResponseFactory> _errorResponseFactory;
        private Mock<IHostSettings> _hostSettingsMock;
        private Mock<QueryTeams.IById> _queryTeamsByIdMock;
        private Mock<QueryUsers.IById> _queryUsersByIdMock;
        private Mock<IRepository<Core.Domain.User>> _userRepositoryMock;
        private Mock<ISlashCommandHandler> _slashCommandHandlerMock;

        [SetUp]
        public void BeforeEachTest()
        {
            _errorResponseFactory = new Mock<ISlashCommandErrorResponseFactory>();
            _hostSettingsMock = new Mock<IHostSettings>();
            _queryTeamsByIdMock = new Mock<QueryTeams.IById>();
            _queryUsersByIdMock = new Mock<QueryUsers.IById>();
            _userRepositoryMock = new Mock<IRepository<Core.Domain.User>>();
            _slashCommandHandlerMock = new Mock<ISlashCommandHandler>();
        }

        private TestBootstrapper GetBootstrapper(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator> withConfig = null)
        {
            return new TestBootstrapper(with =>
            {
                with.Module<TodoModule>();
                with.ViewFactory<TestingViewFactory>();
                with.Dependency<ISlashCommandErrorResponseFactory>(_errorResponseFactory.Object);
                with.Dependency<QueryTeams.IById>(_queryTeamsByIdMock.Object);
                with.Dependency<QueryUsers.IById>(_queryUsersByIdMock.Object);
                with.Dependency<IHostSettings>(_hostSettingsMock.Object);
                with.Dependency<IRepository<Core.Domain.User>>(_userRepositoryMock.Object);
                with.Dependency<ISlashCommandHandler>(_slashCommandHandlerMock.Object);
                with.Dependency<JsonSerializer>(new CustomJsonSerializer());
                if (withConfig != null)
                {
                    withConfig(with);
                }
            });
        }

        [Test]
        public void ReturnsErrorWhenAccountNotFound()
        {
            // Arrange
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.ActiveAccountNotFound()).Returns(expectedResponse);
            _queryTeamsByIdMock.Setup(x => x.ById(It.IsAny<string>())).Returns(Task.FromResult<TeamDto>(null));
            var command = GetSlashCommand(teamId: "slackTeamId");
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + "whatever", with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(expectedResponse.StatusCode));
            Assert.That(result.Body.AsString(), Is.EqualTo(errorMessage));
        }

        [Test]
        public void ReturnsErrorWhenAccountIsNotActive()
        {
            // Arrange
            var teamDto = DtoFactory.Team();
            teamDto.ActivatedAt = null;
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.ActiveAccountNotFound()).Returns(expectedResponse);
            _queryTeamsByIdMock.Setup(x => x.ById(teamDto.Id)).Returns(Task.FromResult(teamDto));
            var command = GetSlashCommand(teamId: teamDto.Id);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + teamDto.Id, with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(expectedResponse.StatusCode));
            Assert.That(result.Body.AsString(), Is.EqualTo(errorMessage));
        }

        [Test]
        public void ReturnsErrorWhenSlashCommandTokenIsInvalid()
        {
            // Arrange
            var teamDto = DtoFactory.Team();
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.InvalidSlashCommandToken()).Returns(expectedResponse);
            _queryTeamsByIdMock.Setup(x => x.ById(teamDto.Id)).Returns(Task.FromResult(teamDto));
            var command = GetSlashCommand(teamId: teamDto.Id, token: "someOtherToken");
            Assert.That(command.Token, Is.Not.EqualTo(teamDto.SlashCommandToken));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + teamDto.Id, with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(expectedResponse.StatusCode));
            Assert.That(result.Body.AsString(), Is.EqualTo(errorMessage));
        }

        [Test]
        public void CreatesUserIfItDoesNotExistAlready()
        {
            // Arrange
            var teamDto = DtoFactory.Team();
            _queryTeamsByIdMock.Setup(x => x.ById(teamDto.Id)).Returns(Task.FromResult(teamDto));
            var savedEvents = new List<IDomainEvent>();
            _userRepositoryMock
                .Setup(x => x.Save(It.IsAny<Core.Domain.User>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Core.Domain.User u) => savedEvents.AddRange(u.GetUncommittedEvents()));
            var command = GetSlashCommand(teamDto);
            _slashCommandHandlerMock.Setup(x => x.Handle(It.IsAny<SlashCommand>()))
                .Returns(Task.FromResult<string>(null));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);
            var before = DateTime.UtcNow;

            // Act
            var result = browser.Post("/api/" + teamDto.Id, with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            _userRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.User>(u => 
                u.TeamId == teamDto.Id)));
            var userCreated = savedEvents.Single(x => x is UserCreated) as UserCreated;
            Assert.That(userCreated.TeamId, Is.EqualTo(teamDto.Id));
            Assert.That(userCreated.Id, Is.EqualTo(command.UserId));
        }

        [Test]
        public void PassesSlashCommandToHandlerAndReturnsResultIfAny()
        {
            // Arrange
            var teamDto = DtoFactory.Team();
            _queryTeamsByIdMock.Setup(x => x.ById(teamDto.Id)).Returns(Task.FromResult(teamDto));
            _userRepositoryMock
                .Setup(x => x.Save(It.IsAny<Core.Domain.User>()))
                .Returns(Task.FromResult<object>(null));
            SlashCommand passedCommand = null;
            string resultText = "result";
            _slashCommandHandlerMock.Setup(x => x.Handle(It.IsAny<SlashCommand>()))
                .Returns(Task.FromResult(resultText))
                .Callback((SlashCommand c) => passedCommand = c);
                
            var command = GetSlashCommand(teamDto);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + teamDto.Id, with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            _slashCommandHandlerMock.Verify(x => x.Handle(It.IsAny<SlashCommand>()), Times.Once);
            passedCommand.AssertIsEqualTo(command);
            Assert.That(result.Body.AsString(), Is.EqualTo(resultText));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        private static SlashCommand GetSlashCommand(
            TeamDto team,
            string userId = "userId",
            string conversationId = "conversationId",
            string text = "text")
        {
            return GetSlashCommand(
                token: team.SlashCommandToken,
                teamId: team.Id,
                userId: userId,
                conversationId: conversationId,
                text: text);
        }

        private static SlashCommand GetSlashCommand(
            string token = "token",
            string teamId = "teamId",
            string userId = "userId",
            string conversationId = "conversationId",
            string text = "text")
        {
            return new SlashCommand
            {
                Token = token,
                TeamId = teamId,
                UserId = userId,
                ConversationId = conversationId,
                Text = text,
                TeamDomain = "teamDomain",
                UserName = "userName",
                ConversationName = "conversation",
                Command = "/todo"
            };
        }
    }

    public static class SlashCommandExtensions
    {
        public static void AssertIsEqualTo(this SlashCommand actualCommand, SlashCommand expectedCommand)
        {
            Assert.That(actualCommand, Is.Not.Null);
            Assert.That(actualCommand.TeamId, Is.EqualTo(expectedCommand.TeamId));
            Assert.That(actualCommand.TeamDomain, Is.EqualTo(expectedCommand.TeamDomain));
            Assert.That(actualCommand.UserId, Is.EqualTo(expectedCommand.UserId));
            Assert.That(actualCommand.UserName, Is.EqualTo(expectedCommand.UserName));
            Assert.That(actualCommand.Token, Is.EqualTo(expectedCommand.Token));
            Assert.That(actualCommand.ConversationId, Is.EqualTo(expectedCommand.ConversationId));
            Assert.That(actualCommand.ConversationName, Is.EqualTo(expectedCommand.ConversationName));
            Assert.That(actualCommand.Text, Is.EqualTo(expectedCommand.Text));
        }
    }
}
