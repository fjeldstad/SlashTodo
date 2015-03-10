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
using Nancy.Security;
using Nancy.Testing;
using NUnit.Framework;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Lookups;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Account;
using SlashTodo.Web.Account.ViewModels;
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
        private Mock<IAccountLookup> _accountLookupMock;
        private Mock<IQueryTeamsById> _accountQueryMock;
        private Mock<IRepository<Core.Domain.Team>> _accountRepositoryMock;
        private AccountKit _accountKit;
        private SlackUserIdentity _userIdentity;

        [SetUp]
        public void BeforeEachTest()
        {
            _appSettingsMock = new Mock<IAppSettings>();
            _hostSettingsMock = new Mock<IHostSettings>();
            _userMapperMock = new Mock<IUserMapper>();
            _accountLookupMock = new Mock<IAccountLookup>();
            _accountQueryMock = new Mock<IQueryTeamsById>();
            _accountRepositoryMock = new Mock<IRepository<Core.Domain.Team>>();
            _accountKit = new AccountKit(
                _accountLookupMock.Object,
                _accountQueryMock.Object,
                _accountRepositoryMock.Object);
            _userIdentity = new SlackUserIdentity
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                SlackUserId = "slackUserId",
                SlackUserName = "slackUserName",
                SlackApiAccessToken = "slackApiAccessToken",
                SlackTeamId = "slackTeamId",
                SlackTeamName = "slackTeamName",
                SlackTeamUrl = new Uri("https://team.slack.com")
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
                with.Dependency<AccountKit>(_accountKit);
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
            var accountDto = new TeamDto
            {
                Id = _userIdentity.AccountId,
                SlackTeamId = _userIdentity.SlackTeamId,
                Name = _userIdentity.SlackTeamName,
                SlackUrl = _userIdentity.SlackTeamUrl,
                SlashCommandToken = "slashCommandToken",
                IncomingWebhookUrl = new Uri("https://api.slack.com/incoming-webhooks/abc"),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = true
            };
            _accountQueryMock.Setup(x => x.Query(_userIdentity.AccountId)).Returns(Task.FromResult(accountDto));
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
            Assert.That(viewModel.SlackTeamName, Is.EqualTo(_userIdentity.SlackTeamName));
            Assert.That(viewModel.SlackTeamUrl, Is.EqualTo(_userIdentity.SlackTeamUrl.AbsoluteUri));
            Assert.That(viewModel.IncomingWebhookUrl, Is.EqualTo(accountDto.IncomingWebhookUrl.AbsoluteUri));
            Assert.That(viewModel.SlashCommandToken, Is.EqualTo(accountDto.SlashCommandToken));
            Assert.That(viewModel.SlashCommandUrl, Is.EqualTo(hostBaseUrl.TrimEnd('/') + "/api/" + _userIdentity.AccountId.ToString("N")));
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
            _accountRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult<Core.Domain.Team>(null));
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
            _accountRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult<Core.Domain.Team>(null));
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
            _accountRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult<Core.Domain.Team>(null));
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
            var account = Core.Domain.Team.Create(_userIdentity.AccountId, _userIdentity.SlackTeamId);
            account.ClearUncommittedEvents();
            _accountRepositoryMock.Setup(x => x.GetById(account.Id)).Returns(Task.FromResult(account));
            var savedEvents = new List<IDomainEvent>();
            _accountRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>())).Returns(Task.FromResult<object>(null)).Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
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
            browser.Post("/account/slash-command-token", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = newSlashCommandToken,
                });
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == account.Id)), Times.Once);
            var slashCommandTokenUpdated = savedEvents.Single(x => x is TeamSlashCommandTokenUpdated) as TeamSlashCommandTokenUpdated;
            slashCommandTokenUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
            Assert.That(slashCommandTokenUpdated.SlashCommandToken, Is.EqualTo(newSlashCommandToken));
        }

        [Test]
        public void UpdatedIncomingWebhookUrlIsSavedToCorrectAccount()
        {
            // Arrange
            var newIncomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook");
            var account = Core.Domain.Team.Create(_userIdentity.AccountId, _userIdentity.SlackTeamId);
            account.ClearUncommittedEvents();
            _accountRepositoryMock.Setup(x => x.GetById(account.Id)).Returns(Task.FromResult(account));
            var savedEvents = new List<IDomainEvent>();
            _accountRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>())).Returns(Task.FromResult<object>(null)).Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
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
                    incomingWebhookUrl = newIncomingWebhookUrl
                });
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == account.Id)), Times.Once);
            var incomingWebhookUpdated = savedEvents.Single(x => x is TeamIncomingWebhookUpdated) as TeamIncomingWebhookUpdated;
            incomingWebhookUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
            Assert.That(incomingWebhookUpdated.IncomingWebhookUrl, Is.EqualTo(newIncomingWebhookUrl));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void UpdatedSlashCommandTokenConvertsEmptyOrWhitespaceTokenToNull(string token)
        {
            // Arrange
            var account = Core.Domain.Team.Create(_userIdentity.AccountId, _userIdentity.SlackTeamId);
            account.UpdateSlashCommandToken("slashCommandToken");
            account.ClearUncommittedEvents();
            _accountRepositoryMock.Setup(x => x.GetById(account.Id)).Returns(Task.FromResult(account));
            var savedEvents = new List<IDomainEvent>();
            _accountRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>())).Returns(Task.FromResult<object>(null)).Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
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
            browser.Post("/account/slash-command-token", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = token,
                });
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == account.Id)), Times.Once);
            var slashCommandTokenUpdated = savedEvents.Single(x => x is TeamSlashCommandTokenUpdated) as TeamSlashCommandTokenUpdated;
            slashCommandTokenUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
            Assert.That(slashCommandTokenUpdated.SlashCommandToken, Is.Null);
        }

        [TestCase("")]
        [TestCase(" ")]
        public void UpdatedIncomingWebhookUrlConvertsEmptyOrWhitespaceUrlToNull(string url)
        {
            // Arrange
            var account = Core.Domain.Team.Create(_userIdentity.AccountId, _userIdentity.SlackTeamId);
            account.UpdateIncomingWebhookUrl(new Uri("https://api.slack.com/incoming-webhook"));
            account.ClearUncommittedEvents();
            _accountRepositoryMock.Setup(x => x.GetById(account.Id)).Returns(Task.FromResult(account));
            var savedEvents = new List<IDomainEvent>();
            _accountRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Team>())).Returns(Task.FromResult<object>(null)).Callback((Core.Domain.Team a) => savedEvents.AddRange(a.GetUncommittedEvents()));
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
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Team>(a => a.Id == account.Id)), Times.Once);
            var incomingWebhookUpdated = savedEvents.Single(x => x is TeamIncomingWebhookUpdated) as TeamIncomingWebhookUpdated;
            incomingWebhookUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
            Assert.That(incomingWebhookUpdated.IncomingWebhookUrl, Is.Null);
        }
    }
}
