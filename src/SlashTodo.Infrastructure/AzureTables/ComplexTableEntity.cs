using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace SlashTodo.Infrastructure.AzureTables
{
    public abstract class ComplexTableEntity<T> : TableEntity
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        public string SerializedData { get; set; }

        protected ComplexTableEntity() { }

        protected ComplexTableEntity(T data, Func<T, string> partitionKey, Func<T, string> rowKey)
        {
            PartitionKey = partitionKey(data);
            RowKey = rowKey(data);
            SerializedData = JsonConvert.SerializeObject(data, SerializerSettings);
        }

        public T GetData()
        {
            return (T)JsonConvert.DeserializeObject(SerializedData, SerializerSettings);
        }
    }
}
