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
using SlashTodo.Infrastructure.AzureTables;
using SlashTodo.Infrastructure.AzureTables.Lookups;
using SlashTodo.Infrastructure.AzureTables.Queries;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests.Storage.AzureTables.Queries
{
    [TestFixture]
    public class AzureTableAccountQueryTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private QueryTeamsById _queryTeamsById;
        private Mock<IAccountLookup> _accountLookupMock;
        private IMessageBus _bus;
            
        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            _accountLookupMock = new Mock<IAccountLookup>();
            // Reference a different table for each test to ensure isolation.
            _queryTeamsById = new QueryTeamsById(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString),
                string.Format("test{0}", Guid.NewGuid().ToString("N")), _accountLookupMock.Object);
            _queryTeamsById.RegisterSubscriptions((ISubscriptionRegistry)_bus);
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
            _queryTeamsById.Dispose();
        }
        private CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(_azureSettings.StorageConnectionString);
            var cloudTableClient = storageAccount.CreateCloudTableClient();
            var table = cloudTableClient.GetTableReference(_queryTeamsById.TableName);
            return table;
        }

        [Test]
        public async Task InsertsNewRowOnAccountCreated()
        {
            // Arrange
            var accountCreated = new TeamCreated
            {
                Id = Guid.NewGuid(),
                SlackTeamId = "slackTeamId",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(accountCreated);

            // Assert
            var table = GetTable();
            var retrieveOp = TableOperation.Retrieve<QueryTeamsById.TeamDtoTableEntity>(accountCreated.Id.ToString(), accountCreated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as QueryTeamsById.TeamDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Id, Is.EqualTo(accountCreated.SlackTeamId));
            Assert.That(row.CreatedAt, Is.EqualTo(accountCreated.Timestamp));
        }

        [Test]
        public async Task UpdatesRowOnAccountSlackTeamInfoUpdated()
        {
            // Arrange
            var dto = GetAccountDto(slackTeamName: "oldSlackTeamName", slackTeamUrl: new Uri("https://oldteam.slack.com"));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountSlackTeamInfoUpdated = new TeamInfoUpdated
            {
                Id = dto.Id,
                Name = "newSlackTeamName",
                SlackUrl = new Uri("https://team.slack.com")
            };

            // Act
            await _bus.Publish(accountSlackTeamInfoUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<QueryTeamsById.TeamDtoTableEntity>(accountSlackTeamInfoUpdated.Id.ToString(), accountSlackTeamInfoUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as QueryTeamsById.TeamDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Name, Is.EqualTo(accountSlackTeamInfoUpdated.Name));
            Assert.That(row.SlackUrl, Is.EqualTo(accountSlackTeamInfoUpdated.SlackUrl.AbsoluteUri));
        }

        [Test]
        public async Task UpdatesRowOnAccountSlashCommandTokenUpdated()
        {
            // Arrange
            var dto = GetAccountDto(slashCommandToken: "oldSlashCommandToken");
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountSlashCommandTokenUpdated = new TeamSlashCommandTokenUpdated
            {
                Id = dto.Id,
                SlashCommandToken = "newSlashCommandToken"
            };

            // Act
            await _bus.Publish(accountSlashCommandTokenUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<QueryTeamsById.TeamDtoTableEntity>(accountSlashCommandTokenUpdated.Id.ToString(), accountSlashCommandTokenUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as QueryTeamsById.TeamDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlashCommandToken, Is.EqualTo(accountSlashCommandTokenUpdated.SlashCommandToken));
        }

        [Test]
        public async Task UpdatesRowOnAccountIncomingWebhookUrlUpdated()
        {
            // Arrange
            var dto = GetAccountDto(incomingWebhookUrl: new Uri("https://api.slack.com/old-incoming-webhook"));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountIncomingWebhookUrlUpdated = new TeamIncomingWebhookUpdated
            {
                Id = dto.Id,
                IncomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
            };

            // Act
            await _bus.Publish(accountIncomingWebhookUrlUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<QueryTeamsById.TeamDtoTableEntity>(accountIncomingWebhookUrlUpdated.Id.ToString(), accountIncomingWebhookUrlUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as QueryTeamsById.TeamDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IncomingWebhookUrl, Is.EqualTo(accountIncomingWebhookUrlUpdated.IncomingWebhookUrl));
        }

        [Test]
        public async Task UpdatesRowOnAccountActivated()
        {
            // Arrange
            var dto = GetAccountDto(isActive: false);
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountActivated = new TeamActivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(accountActivated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<QueryTeamsById.TeamDtoTableEntity>(accountActivated.Id.ToString(), accountActivated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as QueryTeamsById.TeamDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IsActive, Is.True);
        }

        [Test]
        public async Task UpdatesRowOnAccountDeactivated()
        {
            // Arrange
            var dto = GetAccountDto(isActive: true);
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountDeactivated = new TeamDeactivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(accountDeactivated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<QueryTeamsById.TeamDtoTableEntity>(accountDeactivated.Id.ToString(), accountDeactivated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as QueryTeamsById.TeamDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IsActive, Is.False);
        }

        [Test]
        public async Task ByIdReturnsNullWhenQueryFails()
        {
            // Arrange

            // Act
            var accountId = await _queryTeamsById.ById(Guid.NewGuid());

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task BySlackTeamIdReturnsNullWhenLookupFails()
        {
            // Arrange
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(null));

            // Act
            var accountId = await _queryTeamsById.BySlackTeamId("whatever");

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task BySlackTeamIdReturnsNullWhenLookupSucceedsButQueryFails()
        {
            // Arrange
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(Guid.NewGuid()));

            // Act
            var accountId = await _queryTeamsById.BySlackTeamId("whatever");

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task ByIdReturnsAccountDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = GetAccountDto(isActive: true);
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(expectedDto));
            table.Execute(insertOp);

            // Act
            var accountDto = await _queryTeamsById.ById(expectedDto.Id);

            // Assert
            accountDto.AssertIsEqualTo(expectedDto);
        }

        [Test]
        public async Task BySlackTeamIdReturnsAccountDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = GetAccountDto(isActive: true);
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(expectedDto.Id));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new QueryTeamsById.TeamDtoTableEntity(expectedDto));
            table.Execute(insertOp);

            // Act
            var accountDto = await _queryTeamsById.BySlackTeamId(expectedDto.SlackTeamId);

            // Assert
            accountDto.AssertIsEqualTo(expectedDto);
        }

        private static TeamDto GetAccountDto(
            string slackTeamName = "slackTeamName", 
            string slashCommandToken = "slashCommandToken",
            Uri slackTeamUrl = null,
            Uri incomingWebhookUrl = null,
            bool isActive = false)
        {
            return new TeamDto
            {
                Id = Guid.NewGuid(),
                SlackTeamId = "slackTeamId",
                Name = slackTeamName,
                SlackUrl = slackTeamUrl ?? new Uri("https://team.slack.com"),
                SlashCommandToken = slashCommandToken,
                IncomingWebhookUrl = incomingWebhookUrl ?? new Uri("http://api.slack.com/incoming-webhook"),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = isActive
            };
        }
    }
    public static class AccountDtoExtensions
    {
        public static void AssertIsEqualTo(this TeamDto actualDto, TeamDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.SlackTeamId, Is.EqualTo(expectedDto.SlackTeamId));
            Assert.That(actualDto.Name, Is.EqualTo(expectedDto.Name));
            Assert.That(actualDto.SlackUrl, Is.EqualTo(expectedDto.SlackUrl));
            Assert.That(actualDto.SlashCommandToken, Is.EqualTo(expectedDto.SlashCommandToken));
            Assert.That(actualDto.IncomingWebhookUrl, Is.EqualTo(expectedDto.IncomingWebhookUrl));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.IsActive, Is.EqualTo(expectedDto.IsActive));
        }
    }
}
