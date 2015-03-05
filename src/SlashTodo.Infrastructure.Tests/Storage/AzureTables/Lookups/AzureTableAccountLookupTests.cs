using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Storage.AzureTables;
using SlashTodo.Infrastructure.Storage.AzureTables.Lookups;

namespace SlashTodo.Infrastructure.Tests.Storage.AzureTables.Lookups
{
    [TestFixture]
    public class AzureTableAccountLookupTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private AzureTableAccountLookup _accountLookup;

        [SetUp]
        public void BeforeEachTest()
        {
            // Reference a different table for each test to ensure isolation.
            _accountLookup = new AzureTableAccountLookup(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString),
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
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
            var table = cloudTableClient.GetTableReference(_accountLookup.TableName);
            return table;
        }

        [Test]
        public async Task InsertsNewRowOnAccountCreated()
        {
            // Arrange
            var accountCreated = new AccountCreated
            {
                Id = Guid.NewGuid(),
                SlackTeamId = "slackTeamId"
            };

            // Act
            await _accountLookup.HandleEvent(accountCreated);

            // Assert
            var table = GetTable();
            var retrieveOp = TableOperation.Retrieve<LookupAggregateIdByStringTableEntity>(accountCreated.SlackTeamId, accountCreated.SlackTeamId);
            var row = table.Execute(retrieveOp).Result as LookupAggregateIdByStringTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.AggregateId, Is.EqualTo(accountCreated.Id));
        }

        [Test]
        public async Task ReturnsNullWhenLookupFails()
        {
            // Arrange

            // Act
            var accountId = await _accountLookup.BySlackTeamId("whatever");

            // Assert
            Assert.That(accountId, Is.Null);
        }

        [Test]
        public async Task ReturnsAccountIdWhenLookupIsSuccessful()
        {
            // Arrange
            var expectedAccountId = Guid.NewGuid();
            var slackTeamId = "slackTeamId";
            var table = GetTable();
            var insertOp = TableOperation.Insert(new LookupAggregateIdByStringTableEntity(slackTeamId, expectedAccountId));
            table.Execute(insertOp);

            // Act
            var accountId = await _accountLookup.BySlackTeamId(slackTeamId);

            // Assert
            Assert.That(accountId, Is.EqualTo(expectedAccountId));
        }
    }
}
