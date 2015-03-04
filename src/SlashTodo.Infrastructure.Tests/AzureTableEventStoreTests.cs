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
using SlashTodo.Infrastructure.Configuration;

namespace SlashTodo.Infrastructure.Tests
{
    [TestFixture]
    public class AzureTableEventStoreTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private AzureTableEventStore _eventStore;

        [SetUp]
        public void BeforeEachTest()
        {
            // Reference a different table for each test to ensure isolation.
            _eventStore = new AzureTableEventStore(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString), 
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
            var table = GetTableForEventStore();
            table.CreateIfNotExists();
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the table used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            var table = GetTableForEventStore();
            table.DeleteIfExists();
        }

        [Test]
        public async Task GetByIdReturnsEmptySequenceWhenAggregateDoesNotExist()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();

            // Act
            var events = await _eventStore.GetById(aggregateId);

            // Assert
            Assert.That(events, Is.Empty);
        }

        [Test]
        public async Task GetByIdReturnsEventsForValidAggregateId()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var dummyEvent = new DummyDomainEvent { Id = aggregateId, OriginalVersion = 0, Timestamp = DateTime.UtcNow };
            var dummyEntity = new AzureTableEventStore.DomainEventEntity(dummyEvent);
            var table = GetTableForEventStore();
            var insertOp = TableOperation.Insert(dummyEntity);
            table.Execute(insertOp);

            // Act
            var events = await _eventStore.GetById(aggregateId);

            // Assert
            var actualEvent = events.Single();
            Assert.That(actualEvent.Id, Is.EqualTo(dummyEvent.Id));
            Assert.That(actualEvent.OriginalVersion, Is.EqualTo(dummyEvent.OriginalVersion));
            Assert.That(actualEvent.Timestamp, Is.EqualTo(dummyEvent.Timestamp));
        }

        [Test]
        public async Task SaveWritesToAzureTable()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var dummyEvent = new DummyDomainEvent { Id = aggregateId, OriginalVersion = 0, Timestamp = DateTime.UtcNow };
            var dummyEntity = new AzureTableEventStore.DomainEventEntity(dummyEvent);

            // Act
            await _eventStore.Save(aggregateId, 0, new[] { dummyEvent });

            // Assert
            var table = GetTableForEventStore();
            var retrieveOp = TableOperation.Retrieve<AzureTableEventStore.DomainEventEntity>(dummyEntity.PartitionKey, dummyEntity.RowKey);
            var retrieveResult = await table.ExecuteAsync(retrieveOp);
            var actualEntity = retrieveResult.Result as AzureTableEventStore.DomainEventEntity;
            Assert.That(actualEntity.PartitionKey, Is.EqualTo(dummyEntity.PartitionKey));
            Assert.That(actualEntity.RowKey, Is.EqualTo(dummyEntity.RowKey));
        }

        [Test]
        public async Task DeleteRemovesAllEntitiesFromAzureTableForSpecifiedAggregateId()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var dummyEvent = new DummyDomainEvent { Id = aggregateId, OriginalVersion = 0, Timestamp = DateTime.UtcNow };
            var dummyEntity = new AzureTableEventStore.DomainEventEntity(dummyEvent);
            var table = GetTableForEventStore();
            var insertOp = TableOperation.Insert(dummyEntity);
            table.Execute(insertOp);

            // Act
            await _eventStore.Delete(aggregateId);

            // Assert
            table = GetTableForEventStore();
            var retrieveOp = TableOperation.Retrieve<AzureTableEventStore.DomainEventEntity>(dummyEntity.PartitionKey, dummyEntity.RowKey);
            var retrieveResult = await table.ExecuteAsync(retrieveOp);
            Assert.That(retrieveResult.Result, Is.Null);
        }

        private CloudTable GetTableForEventStore()
        {
            var storageAccount = CloudStorageAccount.Parse(_azureSettings.StorageConnectionString);
            var cloudTableClient = storageAccount.CreateCloudTableClient();
            var table = cloudTableClient.GetTableReference(_eventStore.TableName);
            return table;
        }

        public class DummyDomainEvent : IDomainEvent
        {
            public Guid Id { get; set; }
            public DateTime Timestamp { get; set; }
            public int OriginalVersion { get; set; }
        }
    }
}
