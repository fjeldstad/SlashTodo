using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Lookups;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Messaging;
using SlashTodo.Infrastructure.Storage.AzureTables;
using SlashTodo.Infrastructure.Storage.AzureTables.Lookups;
using SlashTodo.Infrastructure.Storage.AzureTables.Queries;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests.Storage.AzureTables.Queries
{
    [TestFixture]
    public class AzureTableUserQueryTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private AzureTableUserQuery _userQuery;
        private Mock<IUserLookup> _userLookupMock;
        private IMessageBus _bus;
            
        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            _userLookupMock = new Mock<IUserLookup>();
            // Reference a different table for each test to ensure isolation.
            _userQuery = new AzureTableUserQuery(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString),
                string.Format("test{0}", Guid.NewGuid().ToString("N")), _userLookupMock.Object);
            _userQuery.RegisterSubscriptions((ISubscriptionRegistry)_bus);
            var table = GetTable();
            table.CreateIfNotExists();
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the table used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            var table = GetTable();
            table.DeleteIfExists();
        }
        private CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(_azureSettings.StorageConnectionString);
            var cloudTableClient = storageAccount.CreateCloudTableClient();
            var table = cloudTableClient.GetTableReference(_userQuery.TableName);
            return table;
        }

        [Test]
        public async Task InsertsNewRowOnUserCreated()
        {
            // Arrange
            var userCreated = new UserCreated
            {
                Id = Guid.NewGuid(),
                SlackUserId = "slackUserId",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(userCreated);

            // Assert
            var table = GetTable();
            var retrieveOp = TableOperation.Retrieve<AzureTableUserQuery.UserDtoTableEntity>(userCreated.Id.ToString(), userCreated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableUserQuery.UserDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlackUserId, Is.EqualTo(userCreated.SlackUserId));
            Assert.That(row.CreatedAt, Is.EqualTo(userCreated.Timestamp));
        }

        [Test]
        public async Task UpdatesRowOnUserSlackUserNameUpdated()
        {
            // Arrange
            var dto = GetUserDto(slackUserName: "oldSlackUserName");
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableUserQuery.UserDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var userSlackUserNameUpdated = new UserSlackUserNameUpdated
            {
                Id = dto.Id,
                SlackUserName = "newSlackUserName"
            };

            // Act
            await _bus.Publish(userSlackUserNameUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableUserQuery.UserDtoTableEntity>(userSlackUserNameUpdated.Id.ToString(), userSlackUserNameUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableUserQuery.UserDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlackUserName, Is.EqualTo(userSlackUserNameUpdated.SlackUserName));
        }

        [Test]
        public async Task UpdatesRowOnUserSlackApiAccessTokenUpdated()
        {
            // Arrange
            var dto = GetUserDto(slackApiAccessToken: "oldSlackApiAccessToken");
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableUserQuery.UserDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var userSlackApiAccessTokenUpdated = new UserSlackApiAccessTokenUpdated
            {
                Id = dto.Id,
                SlackApiAccessToken = "newSlackApiAccessToken"
            };

            // Act
            await _bus.Publish(userSlackApiAccessTokenUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableUserQuery.UserDtoTableEntity>(userSlackApiAccessTokenUpdated.Id.ToString(), userSlackApiAccessTokenUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableUserQuery.UserDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlackApiAccessToken, Is.EqualTo(userSlackApiAccessTokenUpdated.SlackApiAccessToken));
        }

        [Test]
        public async Task UpdatesRowOnUserActivated()
        {
            // Arrange
            var dto = GetUserDto(isActive: false);
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableUserQuery.UserDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var userActivated = new UserActivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(userActivated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableUserQuery.UserDtoTableEntity>(userActivated.Id.ToString(), userActivated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableUserQuery.UserDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IsActive, Is.True);
        }

        [Test]
        public async Task ByIdReturnsNullWhenQueryFails()
        {
            // Arrange

            // Act
            var userId = await _userQuery.ById(Guid.NewGuid());

            // Assert
            Assert.That(userId, Is.Null);
        }

        [Test]
        public async Task BySlackUserIdReturnsNullWhenLookupFails()
        {
            // Arrange
            _userLookupMock.Setup(x => x.BySlackUserId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(null));

            // Act
            var userId = await _userQuery.BySlackUserId("whatever");

            // Assert
            Assert.That(userId, Is.Null);
        }

        [Test]
        public async Task BySlackUserIdReturnsNullWhenLookupSucceedsButQueryFails()
        {
            // Arrange
            _userLookupMock.Setup(x => x.BySlackUserId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(Guid.NewGuid()));

            // Act
            var userId = await _userQuery.BySlackUserId("whatever");

            // Assert
            Assert.That(userId, Is.Null);
        }

        [Test]
        public async Task ByIdReturnsUserDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = GetUserDto(isActive: true);
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableUserQuery.UserDtoTableEntity(expectedDto));
            table.Execute(insertOp);

            // Act
            var userDto = await _userQuery.ById(expectedDto.Id);

            // Assert
            userDto.AssertIsEqualTo(expectedDto);
        }

        [Test]
        public async Task BySlackUserIdReturnsUserDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = GetUserDto(isActive: true);
            _userLookupMock.Setup(x => x.BySlackUserId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(expectedDto.Id));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableUserQuery.UserDtoTableEntity(expectedDto));
            table.Execute(insertOp);

            // Act
            var userDto = await _userQuery.BySlackUserId(expectedDto.SlackUserId);

            // Assert
            userDto.AssertIsEqualTo(expectedDto);
        }

        private static UserDto GetUserDto(
            string slackUserId = "slackUserId",
            string slackUserName = "slackUserName",
            string slackApiAccessToken = "slackApiAccessToken",
            bool isActive = false)
        {
            return new UserDto
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                SlackUserId = "slackUserId",
                SlackUserName = slackUserName,
                SlackApiAccessToken = slackApiAccessToken,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = isActive
            };
        }
    }
    public static class UserDtoExtensions
    {
        public static void AssertIsEqualTo(this UserDto actualDto, UserDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.SlackUserId, Is.EqualTo(expectedDto.SlackUserId));
            Assert.That(actualDto.SlackUserName, Is.EqualTo(expectedDto.SlackUserName));
            Assert.That(actualDto.SlackApiAccessToken, Is.EqualTo(expectedDto.SlackApiAccessToken));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.IsActive, Is.EqualTo(expectedDto.IsActive));
        }
    }
}
