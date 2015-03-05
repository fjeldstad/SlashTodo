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
        private readonly string _tableName;

        public string TableName { get { return _tableName; } }

        public AzureTableEventStore(CloudStorageAccount storageAccount, string tableName)
            : base(storageAccount)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            _tableName = tableName;
        }

        public async Task<IEnumerable<IDomainEvent>> GetById(Guid aggregateId)
        {
            var events = (await RetrievePartition(_tableName, aggregateId.ToString()))
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
            var orderedEvents = events
                .OrderBy(x => x.OriginalVersion)
                .Select(x => new DomainEventTableEntity(x));
            await InsertBatch(orderedEvents, _tableName);
        }

        public async Task Delete(Guid aggregateId)
        {
            await DeletePartition(_tableName, aggregateId.ToString());
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