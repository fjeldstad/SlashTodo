using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlashTodo.Infrastructure.Storage.AzureTables
{
    public abstract class TableStorageBase<TEntity> where TEntity : TableEntity
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;

        protected CloudStorageAccount StorageAccount { get { return _storageAccount; } }
        public string TableName { get { return _tableName; } }

        protected TableStorageBase(CloudStorageAccount storageAccount, string tableName)
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

        protected async Task<CloudTable> GetTable()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }
    }
}
