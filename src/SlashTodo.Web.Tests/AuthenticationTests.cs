using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using Nancy.Helpers;
using Nancy.Testing;
using NUnit.Framework;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Authentication;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class AuthenticationTests
    {
        [Test]
        public void LoginRedirectsToSlackAuthorizationUrl()
        {
            // Arrange
            var viewModelFactory = new Mock<IViewModelFactory>();
            var state = new Mock<IOAuthState>();
            var expectedState = "expectedState";
            state.Setup(x => x.Generate()).Returns(expectedState);
            var slackSettings = new Mock<ISlackSettings>();
            slackSettings.SetupGet(x => x.ClientId).Returns("clientId");
            slackSettings.SetupGet(x => x.OAuthAuthorizationUrl).Returns(new Uri("https://slack.com/oauth/authorize"));
            slackSettings.SetupGet(x => x.OAuthRedirectUrl).Returns(new Uri("https://slashtodo.com/authenticate"));
            slackSettings.SetupGet(x => x.OAuthScope).Returns("scope1,scope2,scope3");
            var bootstrapper = new ConfigurableBootstrapper(with =>
            {
                with.Module<AuthenticationModule>();
                with.Dependency<IViewModelFactory>(viewModelFactory.Object);
                with.Dependency<IOAuthState>(state.Object);
                with.Dependency<ISlackSettings>(slackSettings.Object);
            });
            var browser = new Browser(bootstrapper);

            // Arrange
            var result = browser.Get("/login", with =>
            {
                with.HttpRequest();
            });

            // Assert
            var expectedUrl = new UriBuilder(slackSettings.Object.OAuthAuthorizationUrl);
            var expectedQuery = HttpUtility.ParseQueryString(string.Empty);
            expectedQuery["client_id"] = slackSettings.Object.ClientId;
            expectedQuery["scope"] = slackSettings.Object.OAuthScope;
            expectedQuery["redirect_uri"] = slackSettings.Object.OAuthRedirectUrl.AbsoluteUri;
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
        public void ProperlyHandlesFailedAuthorizationFromSlack()
        {
            // Arrange
            var state = new Mock<IOAuthState>();
            var slackSettings = new Mock<ISlackSettings>();
            var bootstrapper = new TestBootstrapper(with =>
            {
                with.Module<AuthenticationModule>();
                with.ViewFactory<TestingViewFactory>();
                with.Dependency<IRootPathProvider>(new TestRootPathProvider());
                with.Dependency<IViewModelFactory>(new ViewModelFactory());
                with.Dependency<IOAuthState>(state.Object);
                with.Dependency<ISlackSettings>(slackSettings.Object);
            });
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
    }
}
