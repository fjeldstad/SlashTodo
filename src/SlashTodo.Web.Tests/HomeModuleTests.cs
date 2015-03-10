using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using Nancy.Security;
using Nancy.Testing;
using NUnit.Framework;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class HomeModuleTests
    {
        private Mock<IHostSettings> _hostSettingsMock;

        [SetUp]
        public void BeforeEachTest()
        {
            _hostSettingsMock = new Mock<IHostSettings>();
            _hostSettingsMock.SetupGet(x => x.HttpsPort).Returns(443);
        }

        private TestBootstrapper GetBootstrapper(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator> withConfig = null)
        {
            return new TestBootstrapper(with =>
            {
                with.Module<HomeModule>();
                with.ViewFactory<TestingViewFactory>();
                with.Dependency<IRootPathProvider>(new TestRootPathProvider());
                with.Dependency<IViewModelFactory>(new ViewModelFactory());
                with.Dependency<IHostSettings>(_hostSettingsMock.Object);
                if (withConfig != null)
                {
                    withConfig(with);
                }
            });
        }

        [Test]
        public void RedirectsToAccountPageWhenUserIsAuthenticated()
        {
            // Arrange
            var currentUserMock = new Mock<IUserIdentity>();
            currentUserMock.SetupGet(x => x.UserName).Returns("username");
            var bootstrapper = GetBootstrapper(with =>
            {
                with.RequestStartup((requestContainer, requestPipelines, requestContext) =>
                {
                    requestContext.CurrentUser = currentUserMock.Object;
                });
            });
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/", with =>
            {
                with.HttpsRequest();
            });

            // Assert
            result.ShouldHaveRedirectedTo("/account");
        }

        [Test]
        public void DisplaysStartPage()
        {
            // Arrange
            var bootstrapper = GetBootstrapper();
            var browser = new Browser(bootstrapper);

            // Act
            var result = browser.Get("/", with =>
            {
                with.HttpsRequest();
            });

            // Assert
            Assert.That(result.GetViewName(), Is.EqualTo("Start.cshtml"));
        }
    }
}
