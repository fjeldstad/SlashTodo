using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace SlashTodo.Infrastructure.Storage.AzureTables
{
    public abstract class ComplexTableEntity<T> : TableEntity
    {
        public string DataTypeAssemblyQualifiedName { get; set; }
        public string SerializedData { get; set; }

        protected ComplexTableEntity() { }

        protected ComplexTableEntity(T data, Func<T, string> partitionKey, Func<T, string> rowKey)
        {
            PartitionKey = partitionKey(data);
            RowKey = rowKey(data);
            SerializedData = JsonConvert.SerializeObject(data);
            DataTypeAssemblyQualifiedName = data.GetType().AssemblyQualifiedName;
        }

        public T GetData()
        {
            return (T)JsonConvert.DeserializeObject(SerializedData, Type.GetType(DataTypeAssemblyQualifiedName));
        }
    }
}
