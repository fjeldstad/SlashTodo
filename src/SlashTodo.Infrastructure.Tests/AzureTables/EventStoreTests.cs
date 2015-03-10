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
using SlashTodo.Infrastructure.AzureTables;

namespace SlashTodo.Infrastructure.Tests.AzureTables
{
    [TestFixture]
    public class EventStoreTests
    {
        private readonly CloudStorageAccount _storageAccount = CloudStorageAccount.Parse((new AzureSettings(new AppSettings())).StorageConnectionString);
        private EventStore _eventStore;

        [SetUp]
        public void BeforeEachTest()
        {
            // Reference a different table for each test to ensure isolation.
            _eventStore = new EventStore(
                _storageAccount, 
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the table used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            var table = _storageAccount.GetTable(_eventStore.TableName);
            table.DeleteIfExists();
        }

        [Test]
        public async Task GetByIdReturnsEmptySequenceWhenAggregateDoesNotExist()
        {
            // Arrange

            // Act
            var events = await _eventStore.GetById("whatever");

            // Assert
            Assert.That(events, Is.Empty);
        }

        [Test]
        public async Task GetByIdReturnsEventsForValidAggregateId()
        {
            // Arrange
            var aggregateId = "id";
            var dummyEvent = new DummyDomainEvent { Id = aggregateId, OriginalVersion = 0, Timestamp = DateTime.UtcNow };
            var dummyEntity = new EventStore.DomainEventTableEntity(dummyEvent);
            var table = _storageAccount.GetTable(_eventStore.TableName);
            table.Insert(dummyEntity);

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
            var aggregateId = "id";
            var dummyEvent = new DummyDomainEvent { Id = aggregateId, OriginalVersion = 0, Timestamp = DateTime.UtcNow };
            var dummyEntity = new EventStore.DomainEventTableEntity(dummyEvent);

            // Act
            await _eventStore.Save(aggregateId, 0, new[] { dummyEvent });

            // Assert
            var table = _storageAccount.GetTable(_eventStore.TableName);
            var actualEntity = table.Retrieve<EventStore.DomainEventTableEntity>(dummyEntity.PartitionKey, dummyEntity.RowKey);
            Assert.That(actualEntity.PartitionKey, Is.EqualTo(dummyEntity.PartitionKey));
            Assert.That(actualEntity.RowKey, Is.EqualTo(dummyEntity.RowKey));
        }

        [Test]
        public async Task DeleteRemovesAllEntitiesFromAzureTableForSpecifiedAggregateId()
        {
            // Arrange
            var aggregateId = "id";
            var dummyEvent = new DummyDomainEvent { Id = aggregateId, OriginalVersion = 0, Timestamp = DateTime.UtcNow };
            var dummyEntity = new EventStore.DomainEventTableEntity(dummyEvent);
            var table = _storageAccount.GetTable(_eventStore.TableName);
            table.Insert(dummyEntity);

            // Act
            await _eventStore.Delete(aggregateId);

            // Assert
            var actualEntity = table.Retrieve<EventStore.DomainEventTableEntity>(dummyEntity.PartitionKey, dummyEntity.RowKey);
            Assert.That(actualEntity, Is.Null);
        }

        public class DummyDomainEvent : IDomainEvent
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public int OriginalVersion { get; set; }
        }
    }
}
