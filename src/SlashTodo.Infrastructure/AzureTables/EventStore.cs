using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using SlashTodo.Core;
using SlashTodo.Core.Domain;

namespace SlashTodo.Infrastructure.AzureTables
{
    public class EventStore : IEventStore
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;

        public string TableName { get { return _tableName; } }

        public EventStore(CloudStorageAccount storageAccount, string tableName)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _storageAccount = storageAccount;
            _tableName = tableName;
        }

        public async Task<IEnumerable<IDomainEvent>> GetById(string aggregateId)
        {
            var table = await _storageAccount.GetTableAsync(_tableName).ConfigureAwait(false);
            var events = (await table.RetrievePartitionAsync<DomainEventTableEntity>(aggregateId).ConfigureAwait(false))
                .Select(x => x.GetData())
                .OrderBy(x => x.OriginalVersion);
            return events;
        }

        public async Task Save(string aggregateId, int expectedVersion, IEnumerable<IDomainEvent> events)
        {
            // We don't have to actually use the expected version, since the Azure Table infrastructure
            // will return an error if we try to insert records in the partition with an already existing
            // RowKey (domain event version).

            // Inserting events in batches results in each batch being encapsulated in its own transaction.
            // A batch may include up to 100 records, which in practice should be more than enough. This
            // means that events often (always?) are inserted as an atomic operation.
            var orderedEvents = events
                .OrderBy(x => x.OriginalVersion)
                .Select(x => new DomainEventTableEntity(x));
            var table = await _storageAccount.GetTableAsync(_tableName).ConfigureAwait(false);
            await table.InsertBatchAsync(orderedEvents).ConfigureAwait(false);
        }

        public async Task Delete(string aggregateId)
        {
            var table = await _storageAccount.GetTableAsync(_tableName).ConfigureAwait(false);
            await table.DeletePartitionAsync(aggregateId).ConfigureAwait(false);
        }

        public class DomainEventTableEntity : ComplexTableEntity<IDomainEvent>
        {
            public DomainEventTableEntity() { }

            public DomainEventTableEntity(IDomainEvent @event)
                : base(@event, e => e.Id, e => e.OriginalVersion.ToString())
            {
            }
        }
    }
}