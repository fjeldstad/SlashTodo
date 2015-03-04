using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Cryptography;
using Nancy.Helpers;
using Nancy.Routing.Constraints;
using Nancy.Testing;
using NUnit.Framework;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Web.Authentication;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class AuthenticationTests
    {
        private Mock<ISlackSettings> _slackSettingsMock;
        private Mock<IOAuthState> _oAuthStateMock;
        private Mock<ISlackApi> _slackApiMock;
        private Mock<IUserMapper> _userMapperMock;

        [SetUp]
        public void BeforeEachTest()
        {
            _slackSettingsMock = new Mock<ISlackSettings>();
            _oAuthStateMock = new Mock<IOAuthState>();
            _slackApiMock = new Mock<ISlackApi>();
            _userMapperMock = new Mock<IUserMapper>();
        }

        private ConfigurableBootstrapper GetBootstrapper(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator> withConfig = null)
        {
            return new TestBootstrapper(with =>
            {
                with.Module<AuthenticationModule>();
                with.ViewFactory<TestingViewFactory>();
                with.Dependency<IRootPathProvider>(new TestRootPathProvider());
                with.Dependency<IViewModelFactory>(new ViewModelFactory());
                with.Dependency<ISlackSettings>(_slackSettingsMock.Object);
                with.Dependency<IOAuthState>(_oAuthStateMock.Object);
                with.Dependency<ISlackApi>(_slackApiMock.Object);
                with.RequestStartup((requestContainer, requestPipelines, nancyContext) =>
                {
                    FormsAuthentication.Enable(requestPipelines, 
                        new FormsAuthenticationConfiguration
                        {
                            CryptographyConfiguration = CryptographyConfiguration.NoEncryption,
                            UserMapper = _userMapperMock.Object,
                            RedirectUrl = "/login"
                        });
                });
                if (withConfig != null)
                {
                    withConfig(with);
                }
            });
        }

        [Test]
        public void LoginRedirectsToSlackAuthorizationUrl()
        {
            // Arrange
            var expectedState = "expectedState";
            _oAuthStateMock.Setup(x => x.Generate()).Returns(expectedState);
            _slackSettingsMock.SetupGet(x => x.ClientId).Returns("clientId");
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _slackSettingsMock.SetupGet(x => x.OAuthScope).Returns("scope1,scope2,scope3");
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Arrange
            var result = browser.Get("/login", with =>
            {
                with.HttpRequest();
            });

            // Assert
            var expectedUrl = new UriBuilder(_slackSettingsMock.Object.OAuthAuthorizationUrl);
            var expectedQuery = HttpUtility.ParseQueryString(string.Empty);
            expectedQuery["client_id"] = _slackSettingsMock.Object.ClientId;
            expectedQuery["scope"] = _slackSettingsMock.Object.OAuthScope;
            expectedQuery["redirect_uri"] = _slackSettingsMock.Object.OAuthRedirectUrl.AbsoluteUri;
            expectedQuery["state"] = expectedState;
            expectedUrl.Query = expectedQuery.ToString();
            result.ShouldHaveRedirectedTo(location =>
            {
                var actual = new Uri(location);
                if (actual.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) !=
                    expectedUrl.Uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped))
                {
                    return false;
                }
                var actualQuery = HttpUtility.ParseQueryString(actual.Query);
                if (!expectedQuery.AllKeys.OrderBy(x => x).SequenceEqual(actualQuery.AllKeys.OrderBy(x => x)))
                {
                    return false;
                }
                return actualQuery.AllKeys.All(key => actualQuery[key] == expectedQuery[key]);
            });
        }

        [Test]
        public void LogoutExpiresAuthenticationCookieAndRedirectsToStartPage()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/logout", with =>
            {
                with.HttpRequest();
            });

            // Assert
            var authCookie = result.Cookies.Single(x => x.Name == FormsAuthentication.FormsAuthenticationCookieName);
            Assert.That(authCookie.Expires.HasValue);
            Assert.That(authCookie.Expires <= DateTime.Now);
            result.ShouldHaveRedirectedTo("/");
        }

        [Test]
        public void ProperlyHandlesFailedAuthorizationFromSlack()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpRequest();
                with.Query("error", "access_denied");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void ProperlyHandlesIncorrectStateParameter()
        {
            // Arrange
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(false);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpRequest();
                with.Query("code", "whatever");
                with.Query("state", "invalidState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void CallsSlackApiToGetAccessToken()
        {
            // Arrange
            var code = "code";
            _slackSettingsMock.SetupGet(x => x.ClientId).Returns("clientId");
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _slackSettingsMock.SetupGet(x => x.OAuthScope).Returns("scope1,scope2,scope3");
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(true);
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            _slackApiMock.Setup(x => x.OAuthAccess(It.IsAny<OAuthAccessRequest>())).Returns(Task.FromResult(accessResponse));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpRequest();
                with.Query("code", code);
            });

            // Assert
            _slackApiMock.Verify(x => x.OAuthAccess(It.Is<OAuthAccessRequest>(r => 
                r.ClientId == _slackSettingsMock.Object.ClientId &&
                r.ClientSecret == _slackSettingsMock.Object.ClientSecret &&
                r.RedirectUri == _slackSettingsMock.Object.OAuthRedirectUrl.AbsoluteUri &&
                r.Code == code)), Times.Once);
        }

        [Test]
        public void ProperlyHandlesUnsuccessfulAccessTokenRequest()
        {
            // Arrange
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(true);
            var accessResponse = new OAuthAccessResponse
            {
                Ok = false,
                Error = "someError"
            };
            _slackApiMock.Setup(x => x.OAuthAccess(It.IsAny<OAuthAccessRequest>())).Returns(Task.FromResult(accessResponse));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void FetchesTeamAndUserInformationFromSlackAfterSuccessfulAuthentication()
        {
            // Arrange
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(true);
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            _slackApiMock.Setup(x => x.OAuthAccess(It.IsAny<OAuthAccessRequest>())).Returns(Task.FromResult(accessResponse));
            var authTestResponse = new AuthTestResponse
            {
                Ok = true
            };
            _slackApiMock.Setup(x => x.AuthTest(It.IsAny<AuthTestRequest>())).Returns(Task.FromResult(authTestResponse));
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _slackApiMock.Verify(x => x.AuthTest(It.Is<AuthTestRequest>(r =>
                r.AccessToken == accessResponse.AccessToken)), Times.Once);
        }

        [Test]
        public void CreatesAuthenticationCookieAndRedirectsToAccountPageWhenAuthenticationIsSuccessful()
        {
            // Arrange
            var code = "code";
            _slackSettingsMock.SetupGet(x => x.ClientId).Returns("clientId");
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _slackSettingsMock.SetupGet(x => x.OAuthScope).Returns("scope1,scope2,scope3");
            var oAuthAccessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken",
                Scope = _slackSettingsMock.Object.OAuthScope
            };
            _slackApiMock.Setup(x => x.OAuthAccess(It.IsAny<OAuthAccessRequest>())).Returns(Task.FromResult(oAuthAccessResponse));
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(true);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpRequest();
                with.Query("code", code);
            });

            // Assert
            var authCookie = result.Cookies.Single(x => x.Name == FormsAuthentication.FormsAuthenticationCookieName);
            Assert.That(!authCookie.Expires.HasValue || authCookie.Expires.Value > DateTime.Now);
            result.ShouldHaveRedirectedTo("/account");
        }
    }
}
