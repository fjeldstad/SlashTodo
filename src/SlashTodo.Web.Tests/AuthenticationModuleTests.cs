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
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Tests.Common;
using SlashTodo.Web.Authentication;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Lookups;
using SlashTodo.Core.Queries;
using SlashTodo.Web.ViewModels;
using User = SlashTodo.Infrastructure.Slack.User;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class AuthenticationModuleTests
    {
        private Mock<IHostSettings> _hostSettingsMock;
        private Mock<ISlackSettings> _slackSettingsMock;
        private Mock<IOAuthState> _oAuthStateMock;
        private Mock<ISlackApi> _slackApiMock;
        private Mock<IUserMapper> _userMapperMock;
        private Mock<IAccountLookup> _accountLookupMock;
        private Mock<IAccountQuery> _accountQueryMock;
        private Mock<IRepository<Core.Domain.Account>> _accountRepositoryMock;
        private Mock<IUserLookup> _userLookupMock;
        private Mock<IUserQuery> _userQueryMock;
        private Mock<IRepository<Core.Domain.User>> _userRepositoryMock;
        private AccountKit _accountKit;
        private UserKit _userKit;

        [SetUp]
        public void BeforeEachTest()
        {
            _hostSettingsMock = new Mock<IHostSettings>();
            _slackSettingsMock = new Mock<ISlackSettings>();
            _oAuthStateMock = new Mock<IOAuthState>();
            _slackApiMock = new Mock<ISlackApi>();
            _userMapperMock = new Mock<IUserMapper>();
            _accountLookupMock = new Mock<IAccountLookup>();
            _accountQueryMock = new Mock<IAccountQuery>();
            _accountRepositoryMock = new Mock<IRepository<Core.Domain.Account>>();
            _userLookupMock = new Mock<IUserLookup>();
            _userQueryMock = new Mock<IUserQuery>();
            _userRepositoryMock = new Mock<IRepository<Core.Domain.User>>();
            _accountKit = new AccountKit(
                _accountLookupMock.Object,
                _accountQueryMock.Object,
                _accountRepositoryMock.Object);
            _userKit = new UserKit(
                _userLookupMock.Object,
                _userQueryMock.Object,
                _userRepositoryMock.Object);
        }

        private TestBootstrapper GetBootstrapper(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator> withConfig = null)
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
                with.Dependency<AccountKit>(_accountKit);
                with.Dependency<UserKit>(_userKit);
                with.Dependency<IHostSettings>(_hostSettingsMock.Object);
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    FormsAuthentication.Enable(requestPipelines, new FormsAuthenticationConfiguration
                    {
                        CryptographyConfiguration = CryptographyConfiguration.NoEncryption,
                        UserMapper = _userMapperMock.Object,
                        RedirectUrl = "/signin"
                    });
                });
                if (withConfig != null)
                {
                    withConfig(with);
                }
            });
        }

        private void ArrangeAuthenticationFlow(
            bool stateIsValid,
            OAuthAccessResponse oAuthAccessResponse,
            AuthTestResponse authTestResponse,
            UsersInfoResponse usersInfoResponse,
            Core.Domain.Account account,
            Core.Domain.User user)
        {
            _hostSettingsMock.SetupGet(x => x.HttpsPort).Returns(443);
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(stateIsValid);
            _slackApiMock.Setup(x => x.OAuthAccess(It.IsAny<OAuthAccessRequest>())).Returns(Task.FromResult(oAuthAccessResponse));
            _slackApiMock.Setup(x => x.AuthTest(It.IsAny<AuthTestRequest>())).Returns(Task.FromResult(authTestResponse));
            _slackApiMock.Setup(x => x.UsersInfo(It.IsAny<UsersInfoRequest>())).Returns(Task.FromResult(usersInfoResponse));
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult(account != null ? account.Id : (Guid?)null));
            _accountQueryMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult(account.ToDto(DateTime.UtcNow.AddDays(-1))));
            _accountRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult(account));
            _userLookupMock.Setup(x => x.BySlackUserId(It.IsAny<string>())).Returns(Task.FromResult(user != null ? user.Id : (Guid?)null));
            _userQueryMock.Setup(x => x.BySlackUserId(It.IsAny<string>())).Returns(Task.FromResult(user.ToDto(DateTime.UtcNow.AddDays(-1))));
            _userRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>())).Returns(Task.FromResult(user));
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
            var result = browser.Get("/signin", with =>
            {
                with.HttpsRequest();
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
            var result = browser.Get("/signout", with =>
            {
                with.HttpsRequest();
            });

            // Assert
            var authCookie = result.Cookies.Single(x => x.Name == FormsAuthentication.FormsAuthenticationCookieName);
            Assert.That(authCookie.Expires.HasValue);
            Assert.That(authCookie.Expires <= DateTime.Now);
            result.ShouldHaveRedirectedTo("/");
        }

        [Test]
        public void HandlesFailedAuthorizationFromSlack()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("error", "access_denied");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void HandlesIncorrectStateParameter()
        {
            // Arrange
            _oAuthStateMock.Setup(x => x.Validate(It.IsAny<string>())).Returns(false);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "whatever");
                with.Query("state", "invalidState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void RequestsAccessTokenFromSlack()
        {
            // Arrange
            var code = "code";
            _slackSettingsMock.SetupGet(x => x.ClientId).Returns("clientId");
            _slackSettingsMock.SetupGet(x => x.ClientSecret).Returns("clientSecret");
            _slackSettingsMock.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            _slackSettingsMock.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            _slackSettingsMock.SetupGet(x => x.OAuthScope).Returns("scope1,scope2,scope3");
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", code);
                with.Query("state", "state");
            });

            // Assert
            _slackApiMock.Verify(x => x.OAuthAccess(It.Is<OAuthAccessRequest>(r => 
                r.ClientId == _slackSettingsMock.Object.ClientId &&
                r.ClientSecret == _slackSettingsMock.Object.ClientSecret &&
                r.RedirectUri == _slackSettingsMock.Object.OAuthRedirectUrl.AbsoluteUri &&
                r.Code == code)), Times.Once);
        }

        [Test]
        public void HandlesUnsuccessfulAccessTokenRequest()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = false,
                Error = "someError"
            };
            ArrangeAuthenticationFlow(true, accessResponse, null, null, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void RequestsAuthTestFromSlack()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _slackApiMock.Verify(x => x.AuthTest(It.Is<AuthTestRequest>(r =>
                r.AccessToken == accessResponse.AccessToken)), Times.Once);
        }

        [Test]
        public void HandlesUnsuccessfulAuthTestRequest()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = false,
                Error = "someError"
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, null, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void RequestsUserInfoFromSlack()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _slackApiMock.Verify(x => x.UsersInfo(It.Is<UsersInfoRequest>(r =>
                r.AccessToken == accessResponse.AccessToken &&
                r.UserId == authTestResponse.UserId)), Times.Once);
        }

        [Test]
        public void HandlesUnsuccessfulUserInfoRequest()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = false,
                Error = "someError"
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void DeniesAccessIfTheUserIsNotAdmin()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = false
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("AuthenticationFailed.cshtml"));
        }

        [Test]
        public void CreatesAccountIfNotExists()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Account>(a => 
                a.GetUncommittedEvents().First() is AccountCreated &&
                (a.GetUncommittedEvents().First() as AccountCreated).SlackTeamId == authTestResponse.TeamId
                )), Times.Once);
        }

        [Test]
        public void DoesNotCreateAccountIfExists()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            var account = Core.Domain.Account.Create(Guid.NewGuid(), authTestResponse.TeamId);
            account.ClearUncommittedEvents();
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, account, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Account>(a =>
                a.GetUncommittedEvents().Any(e => e is AccountCreated))), Times.Never);
        }

        [Test]
        public void UpdatesAccountSlackTeamName()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _accountRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.Account>(a =>
                a.GetUncommittedEvents().Any(e => e is AccountSlackTeamNameUpdated)
                )), Times.Once);
        }

        [Test]
        public void CreatesUserIfNotExists()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            var account = Core.Domain.Account.Create(Guid.NewGuid(), authTestResponse.TeamId);
            account.ClearUncommittedEvents();
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, account, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _userRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.User>(u =>
                u.GetUncommittedEvents().First() is UserCreated &&
                (u.GetUncommittedEvents().First() as UserCreated).SlackUserId == authTestResponse.UserId &&
                (u.GetUncommittedEvents().First() as UserCreated).AccountId == account.Id
                )), Times.Once);
        }

        [Test]
        public void DoesNotCreateUserIfExists()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            var account = Core.Domain.Account.Create(Guid.NewGuid(), authTestResponse.TeamId);
            account.ClearUncommittedEvents();
            var user = Core.Domain.User.Create(Guid.NewGuid(), account.Id, authTestResponse.UserId);
            user.ClearUncommittedEvents();
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, account, user);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _userRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.User>(a =>
                a.GetUncommittedEvents().Any(e => e is UserCreated))), Times.Never);
        }

        [Test]
        public void UpdatesUserSlackApiAccessToken()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _userRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.User>(u =>
                u.GetUncommittedEvents().Any(e => e is UserSlackApiAccessTokenUpdated) &&
                ((UserSlackApiAccessTokenUpdated)u.GetUncommittedEvents().Single(e => e is UserSlackApiAccessTokenUpdated)).SlackApiAccessToken == accessResponse.AccessToken
                )), Times.Once);
        }

        [Test]
        public void UpdatesUserSlackUserName()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            _userRepositoryMock.Verify(x => x.Save(It.Is<Core.Domain.User>(u =>
                u.GetUncommittedEvents().Any(e => e is UserSlackUserNameUpdated) &&
                ((UserSlackUserNameUpdated)u.GetUncommittedEvents().Single(e => e is UserSlackUserNameUpdated)).SlackUserName == authTestResponse.UserName
                )), Times.Once);
        }

        [Test]
        public void CreatesAuthenticationCookieAndRedirectsToAccountPageWhenAuthenticationIsSuccessful()
        {
            // Arrange
            var accessResponse = new OAuthAccessResponse
            {
                Ok = true,
                AccessToken = "accessToken"
            };
            var authTestResponse = new AuthTestResponse
            {
                Ok = true,
                TeamId = "teamId",
                TeamName = "teamName",
                UserId = "userId",
                UserName = "userName"
            };
            var usersInfoResponse = new UsersInfoResponse
            {
                Ok = true,
                User = new User
                {
                    Id = authTestResponse.UserId,
                    Name = authTestResponse.UserName,
                    IsAdmin = true
                }
            };
            ArrangeAuthenticationFlow(true, accessResponse, authTestResponse, usersInfoResponse, null, null);
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/authenticate", with =>
            {
                with.HttpsRequest();
                with.Query("code", "validCode");
                with.Query("state", "validState");
            });

            // Assert
            var authCookie = result.Cookies.Single(x => x.Name == FormsAuthentication.FormsAuthenticationCookieName);
            Assert.That(!authCookie.Expires.HasValue || authCookie.Expires.Value > DateTime.Now);
            result.ShouldHaveRedirectedTo("/account");
        }
    }
}
