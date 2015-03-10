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
using SlashTodo.Infrastructure.Messaging;
using SlashTodo.Infrastructure.AzureTables;
using SlashTodo.Infrastructure.AzureTables.Lookups;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests.Storage.AzureTables.Lookups
{
    [TestFixture]
    public class AzureTableUserLookupTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private AzureTableUserLookup _userLookup;
        private IMessageBus _bus;

        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            // Reference a different table for each test to ensure isolation.
            _userLookup = new AzureTableUserLookup(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString),
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
            _userLookup.RegisterSubscriptions((ISubscriptionRegistry)_bus);
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
            _userLookup.Dispose();
        }
        private CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(_azureSettings.StorageConnectionString);
            var cloudTableClient = storageAccount.CreateCloudTableClient();
            var table = cloudTableClient.GetTableReference(_userLookup.TableName);
            return table;
        }

        [Test]
        public async Task InsertsNewRowOnUserCreated()
        {
            // Arrange
            var userCreated = new UserCreated
            {
                Id = Guid.NewGuid(),
                SlackUserId = "slackUserId"
            };

            // Act
            await _bus.Publish(userCreated);

            // Assert
            var table = GetTable();
            var retrieveOp = TableOperation.Retrieve<LookupAggregateIdByStringTableEntity>(userCreated.SlackUserId, userCreated.SlackUserId);
            var row = table.Execute(retrieveOp).Result as LookupAggregateIdByStringTableEntity;
            Assert.That(row, Is.Not.Null);
            Assert.That(row.AggregateId, Is.EqualTo(userCreated.Id));
        }

        [Test]
        public async Task ReturnsNullWhenLookupFails()
        {
            // Arrange

            // Act
            var userId = await _userLookup.BySlackUserId("whatever");

            // Assert
            Assert.That(userId, Is.Null);
        }

        [Test]
        public async Task ReturnsUserIdWhenLookupIsSuccessful()
        {
            // Arrange
            var expectedUserId = Guid.NewGuid();
            var slackUserId = "slackUserId";
            var table = GetTable();
            var insertOp = TableOperation.Insert(new LookupAggregateIdByStringTableEntity(slackUserId, expectedUserId));
            table.Execute(insertOp);

            // Act
            var userId = await _userLookup.BySlackUserId(slackUserId);

            // Assert
            Assert.That(userId, Is.EqualTo(expectedUserId));
        }
    }
}
