using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Core;
using SlashTodo.Core.Domain;

namespace SlashTodo.Infrastructure.Storage.AzureTables
{
    public class AzureTableEventStore : TableStorageBase<AzureTableEventStore.DomainEventTableEntity>, IEventStore
    {
        public AzureTableEventStore(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount, tableName)
        {
        }

        public async Task<IEnumerable<IDomainEvent>> GetById(Guid aggregateId)
        {
            var table = await GetTable();
            var events = GetDomainEventEntities(table, aggregateId)
                .Select(x => x.GetData())
                .OrderBy(x => x.OriginalVersion);
            return events;
        }

        public async Task Save(Guid aggregateId, int expectedVersion, IEnumerable<IDomainEvent> events)
        {
            // We don't have to actually use the expected version, since the Azure Table infrastructure
            // will return an error if we try to insert records in the partition with an already existing
            // RowKey (domain event version).

            // Inserting events in batches results in each batch being encapsulated in its own transaction.
            // A batch may include up to 100 records, which in practice should be more than enough. This
            // means that events often (always?) are inserted as an atomic operation.
            var table = await GetTable();
            var orderedEvents = events.OrderBy(x => x.OriginalVersion).ToArray();
            var insertedRows = 0;
            while (insertedRows < orderedEvents.Length)
            {
                var batch = new TableBatchOperation();
                foreach (var @event in orderedEvents.Skip(insertedRows).Take(100))
                {
                    batch.Insert(new DomainEventTableEntity(@event));
                }
                var result = await table.ExecuteBatchAsync(batch);
                insertedRows += result.Count;
            }
        }

        public async Task Delete(Guid aggregateId)
        {
            var table = await GetTable();
            var entities = GetDomainEventEntities(table, aggregateId).ToArray();
            var deletedRows = 0;
            while (deletedRows < entities.Length)
            {
                var batch = new TableBatchOperation();
                foreach (var entity in entities.Skip(deletedRows).Take(100))
                {
                    batch.Delete(entity);
                }
                var result = await table.ExecuteBatchAsync(batch);
                deletedRows += result.Count;
            }
        }

        private IEnumerable<DomainEventTableEntity> GetDomainEventEntities(CloudTable table, Guid aggregateId)
        {
            var query = new TableQuery<DomainEventTableEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey",
                    QueryComparisons.Equal,
                    aggregateId.ToString()));
            return table.ExecuteQuery(query);
        }

        public class DomainEventTableEntity : ComplexTableEntity<IDomainEvent>
        {
            public DomainEventTableEntity() { }

            public DomainEventTableEntity(IDomainEvent @event)
                : base(@event, e => e.Id.ToString(), e => e.OriginalVersion.ToString())
            {
            }
        }
    }
}