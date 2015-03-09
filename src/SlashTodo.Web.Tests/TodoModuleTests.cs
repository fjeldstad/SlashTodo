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
using SlashTodo.Core.Lookups;
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
        private Mock<IAccountQuery> _accountQueryMock;
        private Mock<IUserLookup> _userLookupMock;
        private Mock<IUserQuery> _userQueryMock;
        private Mock<IRepository<Core.Domain.User>> _userRepositoryMock;
        private UserKit _userKit;

        [SetUp]
        public void BeforeEachTest()
        {
            _errorResponseFactory = new Mock<ISlashCommandErrorResponseFactory>();
            _hostSettingsMock = new Mock<IHostSettings>();
            _accountQueryMock = new Mock<IAccountQuery>();
            _userLookupMock = new Mock<IUserLookup>();
            _userQueryMock = new Mock<IUserQuery>();
            _userRepositoryMock = new Mock<IRepository<Core.Domain.User>>();
            _userKit = new UserKit(
                _userLookupMock.Object,
                _userQueryMock.Object,
                _userRepositoryMock.Object);
        }

        private TestBootstrapper GetBootstrapper(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator> withConfig = null)
        {
            return new TestBootstrapper(with =>
            {
                with.Module<TodoModule>();
                with.ViewFactory<TestingViewFactory>();
                with.Dependency<ISlashCommandErrorResponseFactory>(_errorResponseFactory.Object);
                with.Dependency<IAccountQuery>(_accountQueryMock.Object);
                with.Dependency<UserKit>(_userKit);
                with.Dependency<IHostSettings>(_hostSettingsMock.Object);
                with.Dependency<JsonSerializer>(new CustomJsonSerializer());
                if (withConfig != null)
                {
                    withConfig(with);
                }
            });
        }

        [Test]
        public void ReturnsNotFoundWhenAccountIdIsNotAValidGuid()
        {
            // Arrange
            _accountQueryMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<AccountDto>(null));
            _accountQueryMock.Setup(x => x.ById(It.IsAny<Guid>())).Returns(Task.FromResult<AccountDto>(null));
            var command = GetSlashCommand(teamId: "slackTeamId");
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/not-a-guid", with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void ReturnsErrorWhenAccountNotFound()
        {
            // Arrange
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.ActiveAccountNotFound()).Returns(expectedResponse);
            _accountQueryMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<AccountDto>(null));
            _accountQueryMock.Setup(x => x.ById(It.IsAny<Guid>())).Returns(Task.FromResult<AccountDto>(null));
            var command = GetSlashCommand(teamId: "slackTeamId");
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + Guid.NewGuid().ToString("N"), with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(expectedResponse.StatusCode));
            Assert.That(result.Body.AsString(), Is.EqualTo(errorMessage));
        }

        [Test]
        public void ReturnsErrorWhenSlackTeamIdDoesNotMatchedStoredValue()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var accountDto = new AccountDto { Id = accountId, SlackTeamId = "slackTeamId" };
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.InvalidAccountIntegrationSettings()).Returns(expectedResponse);
            _accountQueryMock.Setup(x => x.BySlackTeamId(accountDto.SlackTeamId)).Returns(Task.FromResult(accountDto));
            _accountQueryMock.Setup(x => x.ById(accountId)).Returns(Task.FromResult(accountDto));
            var command = GetSlashCommand(teamId: "someOtherSlackTeamId");
            Assert.That(command.TeamId, Is.Not.EqualTo(accountDto.SlackTeamId));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + accountId.ToString("N"), with =>
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
            var accountDto = new AccountDto { IsActive = false };
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.ActiveAccountNotFound()).Returns(expectedResponse);
            _accountQueryMock.Setup(x => x.BySlackTeamId(accountDto.SlackTeamId)).Returns(Task.FromResult(accountDto));
            _accountQueryMock.Setup(x => x.ById(accountDto.Id)).Returns(Task.FromResult(accountDto));
            var command = GetSlashCommand(teamId: accountDto.SlackTeamId);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + accountDto.Id.ToString("N"), with =>
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
            var accountDto = new AccountDto { IsActive = true, SlashCommandToken = "token" };
            var errorMessage = "errorMessage";
            var expectedResponse = new TextResponse(statusCode: HttpStatusCode.OK, contents: errorMessage);
            _errorResponseFactory.Setup(x => x.InvalidSlashCommandToken()).Returns(expectedResponse);
            _accountQueryMock.Setup(x => x.BySlackTeamId(accountDto.SlackTeamId)).Returns(Task.FromResult(accountDto));
            _accountQueryMock.Setup(x => x.ById(accountDto.Id)).Returns(Task.FromResult(accountDto));
            var command = GetSlashCommand(teamId: accountDto.SlackTeamId, token: "someOtherToken");
            Assert.That(command.Token, Is.Not.EqualTo(accountDto.SlashCommandToken));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/api/" + accountDto.Id.ToString("N"), with =>
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
            var account = GetAccountDto();
            _accountQueryMock.Setup(x => x.BySlackTeamId(account.SlackTeamId)).Returns(Task.FromResult(account));
            _accountQueryMock.Setup(x => x.ById(account.Id)).Returns(Task.FromResult(account));
            _userLookupMock.Setup(x => x.BySlackUserId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(null));
            var savedEvents = new List<IDomainEvent>();
            _userRepositoryMock
                .Setup(x => x.Save(It.IsAny<Core.Domain.User>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Core.Domain.User u) => savedEvents.AddRange(u.GetUncommittedEvents()));
            var command = GetSlashCommand(account);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);
            var before = DateTime.UtcNow;

            // Act
            var result = browser.Post("/api/" + account.Id.ToString("N"), with =>
            {
                with.HttpsRequest();
                with.JsonBody(command);
            });

            // Assert
            _userRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.User>(u => 
                u.AccountId == account.Id)));
            var userCreated = savedEvents.Single(x => x is UserCreated) as UserCreated;
            Assert.That(userCreated.AccountId, Is.EqualTo(account.Id));
            Assert.That(userCreated.SlackUserId, Is.EqualTo(command.UserId));
        }



        private static AccountDto GetAccountDto(Guid? accountId = null, string slackTeamId = "slackTeamId", string slashCommandToken = "token", bool isActive = true)
        {
            return new AccountDto
            {
                Id = accountId ?? Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                SlackTeamId = slackTeamId,
                SlashCommandToken = slashCommandToken,
                IsActive = isActive
            };
        }

        private static SlackSlashCommand GetSlashCommand(
            AccountDto account,
            string userId = "userId",
            string conversationId = "conversationId",
            string text = "text")
        {
            return GetSlashCommand(
                token: account.SlashCommandToken,
                teamId: account.SlackTeamId,
                userId: userId,
                conversationId: conversationId,
                text: text);
        }

        private static SlackSlashCommand GetSlashCommand(
            string token = "token",
            string teamId = "teamId",
            string userId = "userId",
            string conversationId = "conversationId",
            string text = "text")
        {
            return new SlackSlashCommand
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
}
