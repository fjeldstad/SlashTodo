using System;
using System.Collections.Generic;
using System.Linq;
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
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Account;
using SlashTodo.Web.Account.ViewModels;
using SlashTodo.Web.Dtos;
using SlashTodo.Web.Lookups;
using SlashTodo.Web.Queries;
using SlashTodo.Web.ViewModels;

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
            _accountKit = new AccountKit
            {
                Lookup = _accountLookupMock.Object,
                Query = _accountQueryMock.Object,
                Repository = _accountRepositoryMock.Object
            };
            _userIdentity = new SlackUserIdentity
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                SlackUserId = "slackUserId",
                SlackUserName = "slackUserName",
                SlackApiAccessToken = "slackApiAccessToken",
                SlackTeamId = "slackTeamId",
                SlackTeamName = "slackTeamName"
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
                        RedirectUrl = "/login"
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
            result.ShouldHaveRedirectedTo("/login?returnUrl=/account");
        }

        [Test]
        public void DisplaysDashboardWithCorrectAccountInfo()
        {
            // Arrange
            var incomingWebhookUrl = new Uri("https://api.slack.com/incoming-webhooks/abc");
            var slashCommandToken = "slashCommandToken";
            var account = Core.Domain.Account.Create(_userIdentity.AccountId, _userIdentity.SlackTeamId);
            account.UpdateSlackTeamName(_userIdentity.SlackTeamName);
            account.UpdateIncomingWebhookUrl(incomingWebhookUrl);
            account.UpdateSlashCommandToken(slashCommandToken);
            account.ClearUncommittedEvents();
            var accountDto = 
            _accountRepositoryMock.Setup(x => x.GetById(_userIdentity.AccountId)).Returns(Task.FromResult(account));
            _accountQueryMock.Setup(x => x.BySlackTeamId(_userIdentity.SlackTeamId)).Returns(Task.FromResult(account.ToDto()));
            _accountLookupMock.Setup(x => x.BySlackTeamId(_userIdentity.SlackTeamId)).Returns(Task.FromResult((Guid?)account.Id));
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
            Assert.That(viewModel.IncomingWebhookUrl, Is.EqualTo(incomingWebhookUrl.AbsoluteUri));
            Assert.That(viewModel.SlashCommandToken, Is.EqualTo(slashCommandToken));
        }
    }
}
