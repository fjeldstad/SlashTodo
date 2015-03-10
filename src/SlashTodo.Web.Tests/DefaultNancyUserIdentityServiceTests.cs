using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Nancy;
using NUnit.Framework;
using SlashTodo.Infrastructure.AzureTables;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Security;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class DefaultNancyUserIdentityServiceTests
    {
        private readonly CloudStorageAccount _storageAccount = CloudStorageAccount.Parse((new AzureSettings(new AppSettings())).StorageConnectionString);
        private Mock<INancyUserIdentityIdProvider> _idProviderMock;
        private DefaultNancyUserIdentityService _service;
        
        [SetUp]
        public void BeforeEachTest()
        {
            _idProviderMock = new Mock<INancyUserIdentityIdProvider>();
            _service = new DefaultNancyUserIdentityService(
                _storageAccount,
                _idProviderMock.Object,
                string.Format("test{0:N}", Guid.NewGuid()),
                string.Format("test{0:N}", Guid.NewGuid()));
        }

        [TearDown]
        public void AfterEachTest()
        {
            var table = _storageAccount.GetTable(_service.UserIdentityByIdTableName);
            table.DeleteIfExists(); 
            table = _storageAccount.GetTable(_service.UserIdentityBySlackUserIdTableName);
            table.DeleteIfExists();
        }

        [Test]
        public async Task GetOrCreateInsertsRowsWhenNotExistsAndReturnsUserIdentity()
        {
            // Arrange
            var id = Guid.NewGuid();
            _idProviderMock.Setup(x => x.GenerateNewId()).Returns(id);
            var expectedUser = GetUserIdentity(id: id);

            // Act
            var actualUser = await _service.GetOrCreate(expectedUser.SlackUserId, expectedUser.SlackTeamId, expectedUser.SlackUserName);

            // Assert
            _idProviderMock.Verify(x => x.GenerateNewId(), Times.Once);
            actualUser.AssertIsEqualTo(expectedUser);
            var table = _storageAccount.GetTable(_service.UserIdentityByIdTableName);
            table.Retrieve<DefaultNancyUserIdentityService.NancyUserIdentityTableEntity>(id.ToString(), id.ToString())
                .GetUserIdentity()
                .AssertIsEqualTo(expectedUser);
            table = _storageAccount.GetTable(_service.UserIdentityBySlackUserIdTableName);
            table.Retrieve<DefaultNancyUserIdentityService.NancyUserIdentityTableEntity>(expectedUser.SlackUserId, expectedUser.SlackUserId)
                .GetUserIdentity()
                .AssertIsEqualTo(expectedUser);
        }

        [Test]
        public async Task GetOrCreateReturnsUserIdentityWhenUserExists()
        {
            // Arrange
            var expectedUser = GetUserIdentity();
            var table = _storageAccount.GetTable(_service.UserIdentityBySlackUserIdTableName);
            table.Insert(new DefaultNancyUserIdentityService.NancyUserIdentityTableEntity(expectedUser, x => x.SlackUserId));

            // Act
            var actualUser = await _service.GetOrCreate(expectedUser.SlackUserId, expectedUser.SlackTeamId, expectedUser.SlackUserName);

            // Assert
            _idProviderMock.Verify(x => x.GenerateNewId(), Times.Never);
            actualUser.AssertIsEqualTo(expectedUser);
        }

        [Test]
        public void GetUserFromIdentifierReturnsNullWhenUserDoesNotExist()
        {
            // Arrange

            // Act
            var user = _service.GetUserFromIdentifier(Guid.NewGuid(), new NancyContext());

            // Assert
            Assert.That(user, Is.Null);
        }

        private static NancyUserIdentity GetUserIdentity(
            Guid? id = null, 
            string slackUserId = "slackUserId", 
            string slackTeamId = "slackTeamId",
            string slackUserName = "slackUserName")
        {
            return new NancyUserIdentity
            {
                Id = id ?? Guid.NewGuid(),
                SlackUserId = slackUserId,
                SlackTeamId = slackTeamId,
                SlackUserName = slackUserName
            };
        }
    }

    public static class NancyUserIdentityExtensions
    {
        public static void AssertIsEqualTo(this NancyUserIdentity actualUserIdentity, NancyUserIdentity expectedUserIdentity)
        {
            Assert.That(actualUserIdentity, Is.Not.Null);
            Assert.That(actualUserIdentity.Id, Is.EqualTo(expectedUserIdentity.Id));
            Assert.That(actualUserIdentity.SlackUserId, Is.EqualTo(expectedUserIdentity.SlackUserId));
            Assert.That(actualUserIdentity.SlackTeamId, Is.EqualTo(expectedUserIdentity.SlackTeamId));
            Assert.That(actualUserIdentity.SlackUserName, Is.EqualTo(expectedUserIdentity.SlackUserName));
        }
    }
}
