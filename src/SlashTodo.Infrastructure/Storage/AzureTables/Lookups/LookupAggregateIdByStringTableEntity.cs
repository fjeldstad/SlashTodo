using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Lookups
{
    public class LookupAggregateIdByStringTableEntity : TableEntity
    {
        public Guid AggregateId { get; set; }

        public LookupAggregateIdByStringTableEntity() { }

        public LookupAggregateIdByStringTableEntity(string key, Guid aggregateId)
        {
            PartitionKey = key;
            RowKey = key;
            AggregateId = aggregateId;
        }
    }
}
