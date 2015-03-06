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
using SlashTodo.Infrastructure.Storage.AzureTables;
using SlashTodo.Infrastructure.Storage.AzureTables.Lookups;
using SlashTodo.Infrastructure.Storage.AzureTables.Queries;

namespace SlashTodo.Infrastructure.Tests.Storage.AzureTables.Queries
{
    [TestFixture]
    public class AzureTableAccountQueryTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private AzureTableAccountQuery _accountQuery;
        private Mock<IAccountLookup> _accountLookupMock;
            
        [SetUp]
        public void BeforeEachTest()
        {
            _accountLookupMock = new Mock<IAccountLookup>();
            // Reference a different table for each test to ensure isolation.
            _accountQuery = new AzureTableAccountQuery(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString),
                string.Format("test{0}", Guid.NewGuid().ToString("N")), _accountLookupMock.Object);
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
            var table = cloudTableClient.GetTableReference(_accountQuery.TableName);
            return table;
        }

        [Test]
        public async Task InsertsNewRowOnAccountCreated()
        {
            // Arrange
            var accountCreated = new AccountCreated
            {
                Id = Guid.NewGuid(),
                SlackTeamId = "slackTeamId",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _accountQuery.HandleEvent(accountCreated);

            // Assert
            var table = GetTable();
            var retrieveOp = TableOperation.Retrieve<AzureTableAccountQuery.AccountDtoTableEntity>(accountCreated.Id.ToString(), accountCreated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableAccountQuery.AccountDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlackTeamId, Is.EqualTo(accountCreated.SlackTeamId));
            Assert.That(row.CreatedAt, Is.EqualTo(accountCreated.Timestamp));
        }

        [Test]
        public async Task UpdatesRowOnAccountSlackTeamNameUpdated()
        {
            // Arrange
            var dto = GetAccountDto(slackTeamName: "oldSlackTeamName");
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableAccountQuery.AccountDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountSlackTeamNameUpdated = new AccountSlackTeamNameUpdated
            {
                Id = dto.Id,
                SlackTeamName = "newSlackTeamName"
            };

            // Act
            await _accountQuery.HandleEvent(accountSlackTeamNameUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableAccountQuery.AccountDtoTableEntity>(accountSlackTeamNameUpdated.Id.ToString(), accountSlackTeamNameUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableAccountQuery.AccountDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlackTeamName, Is.EqualTo(accountSlackTeamNameUpdated.SlackTeamName));
        }

        [Test]
        public async Task UpdatesRowOnAccountSlashCommandTokenUpdated()
        {
            // Arrange
            var dto = GetAccountDto(slashCommandToken: "oldSlashCommandToken");
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableAccountQuery.AccountDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountSlashCommandTokenUpdated = new AccountSlashCommandTokenUpdated
            {
                Id = dto.Id,
                SlashCommandToken = "newSlashCommandToken"
            };

            // Act
            await _accountQuery.HandleEvent(accountSlashCommandTokenUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableAccountQuery.AccountDtoTableEntity>(accountSlashCommandTokenUpdated.Id.ToString(), accountSlashCommandTokenUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableAccountQuery.AccountDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlashCommandToken, Is.EqualTo(accountSlashCommandTokenUpdated.SlashCommandToken));
        }

        [Test]
        public async Task UpdatesRowOnAccountIncomingWebhookUrlUpdated()
        {
            // Arrange
            var dto = GetAccountDto(incomingWebhookUrl: new Uri("https://api.slack.com/old-incoming-webhook"));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableAccountQuery.AccountDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountIncomingWebhookUrlUpdated = new AccountIncomingWebhookUpdated
            {
                Id = dto.Id,
                IncomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
            };

            // Act
            await _accountQuery.HandleEvent(accountIncomingWebhookUrlUpdated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableAccountQuery.AccountDtoTableEntity>(accountIncomingWebhookUrlUpdated.Id.ToString(), accountIncomingWebhookUrlUpdated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableAccountQuery.AccountDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IncomingWebhookUrl, Is.EqualTo(accountIncomingWebhookUrlUpdated.IncomingWebhookUrl));
        }

        [Test]
        public async Task UpdatesRowOnAccountActivated()
        {
            // Arrange
            var dto = GetAccountDto(activatedAt: null);
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableAccountQuery.AccountDtoTableEntity(dto));
            await table.ExecuteAsync(insertOp);
            var accountActivated = new AccountActivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _accountQuery.HandleEvent(accountActivated);

            // Assert
            var retrieveOp = TableOperation.Retrieve<AzureTableAccountQuery.AccountDtoTableEntity>(accountActivated.Id.ToString(), accountActivated.Id.ToString());
            var row = table.Execute(retrieveOp).Result as AzureTableAccountQuery.AccountDtoTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.ActivatedAt, Is.EqualTo(accountActivated.Timestamp));
        }

        [Test]
        public async Task ByIdReturnsNullWhenQueryFails()
        {
            // Arrange

            // Act
            var accountId = await _accountQuery.ById(Guid.NewGuid());

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task BySlackTeamIdReturnsNullWhenLookupFails()
        {
            // Arrange
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(null));

            // Act
            var accountId = await _accountQuery.BySlackTeamId("whatever");

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task BySlackTeamIdReturnsNullWhenLookupSucceedsButQueryFails()
        {
            // Arrange
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(Guid.NewGuid()));

            // Act
            var accountId = await _accountQuery.BySlackTeamId("whatever");

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task ByIdReturnsAccountDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = GetAccountDto(activatedAt: DateTime.UtcNow.AddDays(-1));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableAccountQuery.AccountDtoTableEntity(expectedDto));
            table.Execute(insertOp);

            // Act
            var accountDto = await _accountQuery.ById(expectedDto.Id);

            // Assert
            accountDto.AssertIsEqualTo(expectedDto);
        }

        [Test]
        public async Task BySlackTeamIdReturnsAccountDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = GetAccountDto(activatedAt: DateTime.UtcNow.AddDays(-1));
            _accountLookupMock.Setup(x => x.BySlackTeamId(It.IsAny<string>())).Returns(Task.FromResult<Guid?>(expectedDto.Id));
            var table = GetTable();
            var insertOp = TableOperation.Insert(new AzureTableAccountQuery.AccountDtoTableEntity(expectedDto));
            table.Execute(insertOp);

            // Act
            var accountDto = await _accountQuery.BySlackTeamId(expectedDto.SlackTeamId);

            // Assert
            accountDto.AssertIsEqualTo(expectedDto);
        }

        private static AccountDto GetAccountDto(
            string slackTeamName = "slackTeamName", 
            string slashCommandToken = "slashCommandToken",
            Uri incomingWebhookUrl = null,
            DateTime? activatedAt = null)
        {
            return new AccountDto
            {
                Id = Guid.NewGuid(),
                SlackTeamId = "slackTeamId",
                SlackTeamName = slackTeamName,
                SlashCommandToken = slashCommandToken,
                IncomingWebhookUrl = incomingWebhookUrl ?? new Uri("http://api.slack.com/incoming-webhook"),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ActivatedAt = activatedAt
            };
        }
    }
    public static class AccountDtoExtensions
    {
        public static void AssertIsEqualTo(this AccountDto actualDto, AccountDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.SlackTeamId, Is.EqualTo(expectedDto.SlackTeamId));
            Assert.That(actualDto.SlackTeamName, Is.EqualTo(expectedDto.SlackTeamName));
            Assert.That(actualDto.SlashCommandToken, Is.EqualTo(expectedDto.SlashCommandToken));
            Assert.That(actualDto.IncomingWebhookUrl, Is.EqualTo(expectedDto.IncomingWebhookUrl));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.ActivatedAt, Is.EqualTo(expectedDto.ActivatedAt));
        }
    }
}
