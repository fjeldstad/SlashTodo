using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Cryptography;
using Nancy.Testing;
using NUnit.Framework;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Account;
using SlashTodo.Web.Account.ViewModels;
using SlashTodo.Web.Security;
using SlashTodo.Web.ViewModels;
using SlashTodo.Tests.Common;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class AccountModuleTests
    {
        private Mock<IAppSettings> _appSettingsMock;
        private Mock<IHostSettings> _hostSettingsMock;
        private Mock<IUserMapper> _userMapperMock;
        private Mock<QueryTeams.IById> _queryTeamsByIdMock;
        private Mock<IRepository<Core.Domain.Team>> _teamRepositoryMock;
        private NancyUserIdentity _userIdentity;

        [SetUp]
        public void BeforeEachTest()
        {
            _appSettingsMock = new Mock<IAppSettings>();
            _hostSettingsMock = new Mock<IHostSettings>();
            _userMapperMock = new Mock<IUserMapper>();
            _queryTeamsByIdMock = new Mock<QueryTeams.IById>();
            _teamRepositoryMock = new Mock<IRepository<Core.Domain.Team>>();
            _userIdentity = new NancyUserIdentity
            {
                Id = Guid.NewGuid(),
                SlackUserId = "slackUserId",
                SlackUserName = "slackUserName",
                SlackTeamId = "slackTeamId"
            };
        }

        private TestBootstrapper GetBootstrapper(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator> withConfig = null)
        {
            return new TestBootstrapper(with =>
            {
                with.Module<AccountModule>();
                with.ViewFactory<TestingViewFactory>();
                with.Dependency<IRootPathProvider>(new TestRootPathProvider());
                with.Dependency<IViewModelFactory>(new ViewModelFactory());
                with.Dependency<QueryTeams.IById>(_queryTeamsByIdMock.Object);
                with.Dependency<IRepository<Core.Domain.Team>>(_teamRepositoryMock.Object);
                with.Dependency<IAppSettings>(_appSettingsMock.Object);
                with.Dependency<IHostSettings>(_hostSettingsMock.Object);
                if (withConfig != null)
                {
                    withConfig(with);
                }
            });
        }

        [Test]
        public void RedirectsToLoginWhenUserNotAuthenticated()
        {
            // Arrange
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    FormsAuthentication.Enable(requestPipelines, new FormsAuthenticationConfiguration
                    {
                        CryptographyConfiguration = CryptographyConfiguration.NoEncryption,
                        UserMapper = _userMapperMock.Object,
                        RedirectUrl = "/signin"
                    });
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/account", with =>
            {
                with.HttpsRequest();
            });

            // Assert
            result.ShouldHaveRedirectedTo("/signin?returnUrl=/account");
        }

        [Test]
        public void DisplaysDashboardWithCorrectAccountInfo()
        {
            // Arrange
            var helpEmailAddress = "help@slashtodo.com";
            _appSettingsMock.Setup(x => x.Get("misc:HelpEmailAddress")).Returns(helpEmailAddress);
            var hostBaseUrl = "https://slashtodo.com/";
            _hostSettingsMock.SetupGet(x => x.BaseUrl).Returns(hostBaseUrl);
            var teamDto = DtoFactory.Team(id: _userIdentity.SlackTeamId);
            _queryTeamsByIdMock.Setup(x => x.ById(_userIdentity.SlackTeamId)).Returns(Task.FromResult(teamDto));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);
            
            // Act
            var result = browser.Get("/account", with =>
            {
                with.HttpsRequest();
            });

            // Assert
            Assert.That(result.GetViewName().Equals("Dashboard.cshtml"));
            var viewModel = result.GetModel<DashboardViewModel>();
            Assert.That(viewModel, Is.Not.Null);
            Assert.That(viewModel.SlackTeamName, Is.EqualTo(teamDto.Name));
            Assert.That(viewModel.SlackTeamUrl, Is.EqualTo(teamDto.SlackUrl.AbsoluteUri));
            Assert.That(viewModel.IncomingWebhookUrl, Is.EqualTo(teamDto.IncomingWebhookUrl.AbsoluteUri));
            Assert.That(viewModel.SlashCommandToken, Is.EqualTo(teamDto.SlashCommandToken));
            Assert.That(viewModel.SlashCommandUrl, Is.EqualTo(hostBaseUrl.TrimEnd('/') + "/api/" + _userIdentity.SlackTeamId));
            Assert.That(viewModel.HelpEmailAddress, Is.EqualTo(helpEmailAddress));
        }

        [Test]
        public void UpdateSlashCommandTokenRequiresAuthenticatedUser()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/slash-command-token", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = "newSlashCommandToken",
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void UpdateIncomingWebhookUrlRequiresAuthenticatedUser()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/incoming-webhook-url", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    incomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void UpdateSlashCommandTokenReturnsNotFoundWhenAccountIsNotFound()
        {
            // Arrange
            _teamRepositoryMock.Setup(x => x.GetById(It.IsAny<string>())).Returns(Task.FromResult<Core.Domain.Team>(null));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/slash-command-token", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = "newSlashCommandToken",
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void UpdateIncomingWebhookUrlReturnsNotFoundWhenAccountIsNotFound()
        {
            // Arrange
            _teamRepositoryMock.Setup(x => x.GetById(It.IsAny<string>())).Returns(Task.FromResult<Core.Domain.Team>(null));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/incoming-webhook-url", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    incomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void UpdateIncomingWebhookUrlReturnsBadRequestForNonemptyInvalidUrl()
        {
            // Arrange
            _teamRepositoryMock.Setup(x => x.GetById(It.IsAny<string>())).Returns(Task.FromResult<Core.Domain.Team>(null));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/incoming-webhook-url", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    incomingWebhookUrl = "not a valid url"
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void UpdatedSlashCommandTokenIsSavedToCorrectAccount()
        {
            // Arrange
            var newSlashCommandToken = "newSlashCommandToken";
            var team = Core.Domain.Team.Create(id: _userIdentity.SlackTeamId);
            team.ClearUncommittedEvents();
            _teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(Task.FromResult(team));
            var savedEvents = new List<IDomainEvent>();
            _teamRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);
            var before = DateTime.UtcNow;

            // Act
            browser.Post("/account/slash-command-token", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = newSlashCommandToken,
                });
            });

            // Assert
            _teamRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == team.Id)), Times.Once);
            var slashCommandTokenUpdated = savedEvents.Single(x => x is TeamSlashCommandTokenUpdated) as TeamSlashCommandTokenUpdated;
            slashCommandTokenUpdated.AssertThatBasicEventDataIsCorrect(team.Id, before);
            Assert.That(slashCommandTokenUpdated.SlashCommandToken, Is.EqualTo(newSlashCommandToken));
        }

        [Test]
        public void UpdatedIncomingWebhookUrlIsSavedToCorrectAccount()
        {
            // Arrange
            var newIncomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook");
            var team = Core.Domain.Team.Create(id: _userIdentity.SlackTeamId);
            team.ClearUncommittedEvents();
            _teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(Task.FromResult(team));
            var savedEvents = new List<IDomainEvent>();
            _teamRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);
            var before = DateTime.UtcNow;

            // Act
            browser.Post("/account/incoming-webhook-url", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    incomingWebhookUrl = newIncomingWebhookUrl
                });
            });

            // Assert
            _teamRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == team.Id)), Times.Once);
            var incomingWebhookUpdated = savedEvents.Single(x => x is TeamIncomingWebhookUpdated) as TeamIncomingWebhookUpdated;
            incomingWebhookUpdated.AssertThatBasicEventDataIsCorrect(team.Id, before);
            Assert.That(incomingWebhookUpdated.IncomingWebhookUrl, Is.EqualTo(newIncomingWebhookUrl));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void UpdatedSlashCommandTokenConvertsEmptyOrWhitespaceTokenToNull(string token)
        {
            // Arrange
            var team = Core.Domain.Team.Create(id: _userIdentity.SlackTeamId);
            team.UpdateSlashCommandToken("slashCommandToken");
            team.ClearUncommittedEvents();
            _teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(Task.FromResult(team));
            var savedEvents = new List<IDomainEvent>();
            _teamRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);
            var originalVersion = team.Version;
            var before = DateTime.UtcNow;

            // Act
            browser.Post("/account/slash-command-token", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = token,
                });
            });

            // Assert
            _teamRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == team.Id)), Times.Once);
            var slashCommandTokenUpdated = savedEvents.Single(x => x is TeamSlashCommandTokenUpdated) as TeamSlashCommandTokenUpdated;
            slashCommandTokenUpdated.AssertThatBasicEventDataIsCorrect(team.Id, before);
            Assert.That(slashCommandTokenUpdated.SlashCommandToken, Is.Null);
        }

        [TestCase("")]
        [TestCase(" ")]
        public void UpdatedIncomingWebhookUrlConvertsEmptyOrWhitespaceUrlToNull(string url)
        {
            // Arrange
            var account = Core.Domain.Team.Create(id: _userIdentity.SlackTeamId);
            account.UpdateIncomingWebhookUrl(new Uri("https://api.slack.com/incoming-webhook"));
            account.ClearUncommittedEvents();
            _teamRepositoryMock.Setup(x => x.GetById(account.Id)).Returns(Task.FromResult(account));
            var savedEvents = new List<IDomainEvent>();
            _teamRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);
            var originalVersion = account.Version;
            var before = DateTime.UtcNow;

            // Act
            browser.Post("/account/incoming-webhook-url", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    incomingWebhookUrl = url,
                });
            });

            // Assert
            _teamRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == account.Id)), Times.Once);
            var incomingWebhookUpdated = savedEvents.Single(x => x is TeamIncomingWebhookUpdated) as TeamIncomingWebhookUpdated;
            incomingWebhookUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
            Assert.That(incomingWebhookUpdated.IncomingWebhookUrl, Is.Null);
        }
    }
}
