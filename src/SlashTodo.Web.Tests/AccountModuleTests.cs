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
        private Mock<IHostSettings> _hostSettingsMock;
        private Mock<IUserMapper> _userMapperMock;
        private Mock<IAccountLookup> _accountLookupMock;
        private Mock<IAccountQuery> _accountQueryMock;
        private Mock<IRepository<Core.Domain.Account>> _accountRepositoryMock;
        private AccountKit _accountKit;
        private SlackUserIdentity _userIdentity;

        [SetUp]
        public void BeforeEachTest()
        {
            _hostSettingsMock = new Mock<IHostSettings>();
            _userMapperMock = new Mock<IUserMapper>();
            _accountLookupMock = new Mock<IAccountLookup>();
            _accountQueryMock = new Mock<IAccountQuery>();
            _accountRepositoryMock = new Mock<IRepository<Core.Domain.Account>>();
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
            var hostBaseUrl = "https://slashtodo.com/api/";
            _hostSettingsMock.SetupGet(x => x.ApiBaseUrl).Returns(hostBaseUrl);
            var accountDto = new AccountDto
            {
                Id = _userIdentity.AccountId,
                SlackTeamId = _userIdentity.SlackTeamId,
                SlackTeamName = _userIdentity.SlackTeamName,
                SlackTeamUrl = _userIdentity.SlackTeamUrl,
                SlashCommandToken = "slashCommandToken",
                IncomingWebhookUrl = new Uri("https://api.slack.com/incoming-webhooks/abc"),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ActivatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _accountQueryMock.Setup(x => x.ById(_userIdentity.AccountId)).Returns(Task.FromResult(accountDto));
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
            Assert.That(viewModel.SlashCommandUrl, Is.EqualTo(hostBaseUrl.TrimEnd('/') + "/" + _userIdentity.AccountId.ToString("N")));
        }

        [Test]
        public void NewAccountSettingsRequiresAuthenticatedUser()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/settings", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = "newSlashCommandToken",
                    incomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void NewAccountSettingsReturnsNotFoundWhenAccountIsNotFound()
        {
            // Arrange
            _accountRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult<Core.Domain.Account>(null));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/settings", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = "newSlashCommandToken",
                    incomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
                });
            });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void NewAccountSettingsReturnsBadRequestWhenIncomingWebhookUrlIsNotValidUrl()
        {
            // Arrange
            _accountRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult<Core.Domain.Account>(null));
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = _userIdentity;
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Post("/account/settings", with =>
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
        public void NewAccountSettingsAreSavedToCorrectAccount()
        {
            // Arrange
            var account = Core.Domain.Account.Create(_userIdentity.AccountId, _userIdentity.SlackTeamId);
            account.ClearUncommittedEvents();
            _accountRepositoryMock.Setup(x => x.GetById(account.Id)).Returns(Task.FromResult(account));
            var savedEvents = new List<IDomainEvent>();
            _accountRepositoryMock.Setup(x => x.Save(It.IsAny<Core.Domain.Account>())).Returns(Task.FromResult<object>(null)).Callback((Core.Domain.Account a) => savedEvents.AddRange(a.GetUncommittedEvents()));
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
            browser.Post("/account/settings", with =>
            {
                with.HttpsRequest();
                with.JsonBody(new
                {
                    slashCommandToken = "newSlashCommandToken",
                    incomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
                });
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Account>(a => a.Id == account.Id)), Times.Once);
            var incomingWebhookUpdated = savedEvents.Single(x => x is AccountIncomingWebhookUpdated) as AccountIncomingWebhookUpdated;
            incomingWebhookUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
            var slashCommandTokenUpdated = savedEvents.Single(x => x is AccountSlashCommandTokenUpdated) as AccountSlashCommandTokenUpdated;
            slashCommandTokenUpdated.AssertThatBasicEventDataIsCorrect(account.Id, before);
        }
    }
}
