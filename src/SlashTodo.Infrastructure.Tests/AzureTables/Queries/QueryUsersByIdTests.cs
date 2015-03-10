using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Infrastructure.AzureTables;
using SlashTodo.Infrastructure.AzureTables.Queries.Entities;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Messaging;
using SlashTodo.Infrastructure.AzureTables.Queries;
using SlashTodo.Tests.Common;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests.AzureTables.Queries
{
    [TestFixture]
    public class QueryUsersByIdTests
    {
        private readonly CloudStorageAccount _storageAccount = CloudStorageAccount.Parse((new AzureSettings(new AppSettings())).StorageConnectionString);
        private string _tableName;
        private QueryUsersById _userQuery;
        private IMessageBus _bus;
            
        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            // Reference a different table for each test to ensure isolation.
            _userQuery = new QueryUsersById(
                _storageAccount,
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
            _userQuery.RegisterSubscriptions((ISubscriptionRegistry)_bus);
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the table used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            _storageAccount.GetTable(_userQuery.TableName).DeleteIfExists();
        }

        [Test]
        public async Task InsertsNewRowOnUserCreated()
        {
            // Arrange
            var userCreated = new UserCreated
            {
                Id = "id",
                TeamId = "teamId",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(userCreated);

            // Assert
            var table = _storageAccount.GetTable(_userQuery.TableName);
            var row = table.Retrieve<UserDtoTableEntity>(userCreated.Id, userCreated.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.TeamId, Is.EqualTo(userCreated.TeamId));
            Assert.That(row.CreatedAt, Is.EqualTo(userCreated.Timestamp));
        }

        [Test]
        public async Task UpdatesRowOnUserNameUpdated()
        {
            // Arrange
            var dto = DtoFactory.User(name: "oldName");
            var table = _storageAccount.GetTable(_userQuery.TableName);
            table.Insert(new UserDtoTableEntity(dto));
            var userSlackUserNameUpdated = new UserNameUpdated
            {
                Id = dto.Id,
                Name = "newName"
            };

            // Act
            await _bus.Publish(userSlackUserNameUpdated);

            // Assert
            var row = table.Retrieve<UserDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Name, Is.EqualTo(userSlackUserNameUpdated.Name));
        }

        [Test]
        public async Task UpdatesRowOnUserSlackApiAccessTokenUpdated()
        {
            // Arrange
            var dto = DtoFactory.User(slackApiAccessToken: "oldSlackApiAccessToken");
            var table = _storageAccount.GetTable(_userQuery.TableName);
            table.Insert(new UserDtoTableEntity(dto));
            var userSlackApiAccessTokenUpdated = new UserSlackApiAccessTokenUpdated
            {
                Id = dto.Id,
                SlackApiAccessToken = "newSlackApiAccessToken"
            };

            // Act
            await _bus.Publish(userSlackApiAccessTokenUpdated);

            // Assert
            var row = table.Retrieve<UserDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlackApiAccessToken, Is.EqualTo(userSlackApiAccessTokenUpdated.SlackApiAccessToken));
        }

        [Test]
        public async Task UpdatesRowOnUserActivated()
        {
            // Arrange
            var dto = DtoFactory.User(activatedAt: null);
            var table = _storageAccount.GetTable(_userQuery.TableName);
            table.Insert(new UserDtoTableEntity(dto));
            var userActivated = new UserActivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(userActivated);

            // Assert
            var row = table.Retrieve<UserDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.ActivatedAt, Is.EqualTo(userActivated.Timestamp));
        }

        [Test]
        public async Task ByIdReturnsNullWhenQueryFails()
        {
            // Arrange

            // Act
            var user = await _userQuery.ById("whatever");

            // Assert
            Assert.That(user, Is.Null);
        }

        [Test]
        public async Task ByIdReturnsDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = DtoFactory.User(activatedAt: DateTime.UtcNow.AddDays(-1));
            var table = _storageAccount.GetTable(_userQuery.TableName);
            table.Insert(new UserDtoTableEntity(expectedDto));

            // Act
            var userDto = await _userQuery.ById(expectedDto.Id);

            // Assert
            userDto.AssertIsEqualTo(expectedDto);
        }
    }

    public static class UserDtoExtensions
    {
        public static void AssertIsEqualTo(this UserDto actualDto, UserDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.TeamId, Is.EqualTo(expectedDto.TeamId));
            Assert.That(actualDto.Name, Is.EqualTo(expectedDto.Name));
            Assert.That(actualDto.SlackApiAccessToken, Is.EqualTo(expectedDto.SlackApiAccessToken));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.ActivatedAt, Is.EqualTo(expectedDto.ActivatedAt));
        }
    }
}
