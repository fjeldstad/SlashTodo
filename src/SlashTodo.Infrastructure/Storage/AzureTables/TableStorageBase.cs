using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlashTodo.Infrastructure.Storage.AzureTables
{
    public abstract class TableStorageBase<TEntity> where TEntity : TableEntity, new()
    {
        private readonly CloudStorageAccount _storageAccount;

        protected CloudStorageAccount StorageAccount { get { return _storageAccount; } }

        protected TableStorageBase(CloudStorageAccount storageAccount)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }
            _storageAccount = storageAccount;
        }

        protected async Task<CloudTable> GetTable(string tableName)
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return table;
        }

        protected async Task Insert(TEntity entity, string tableName)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            var insertOperation = TableOperation.Insert(entity);
            var table = await GetTable(tableName).ConfigureAwait(false);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        protected async Task InsertBatch(IEnumerable<TEntity> entities, string tableName)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            var table = await GetTable(tableName).ConfigureAwait(false);
            var entityArray = entities.ToArray();
            var insertedRows = 0;
            while (insertedRows < entityArray.Length)
            {
                var batch = new TableBatchOperation();
                foreach (var entity in entityArray.Skip(insertedRows).Take(100))
                {
                    batch.Insert(entity);
                }
                var result = await table.ExecuteBatchAsync(batch).ConfigureAwait(false);
                insertedRows += result.Count;
            }
        }

        protected async Task<TEntity> Retrieve(string tableName, string partitionKey, string rowKey)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException("rowKey");
            }
            var table = await GetTable(tableName).ConfigureAwait(false);
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            return (await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false)).Result as TEntity;
        }

        protected async Task<IEnumerable<TEntity>> RetrievePartition(string tableName, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            var table = await GetTable(tableName).ConfigureAwait(false);
            var query = new TableQuery<TEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey",
                    QueryComparisons.Equal,
                    partitionKey));
            return table.ExecuteQuery(query);
        }

        protected async Task Update(string tableName, TEntity entity)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var updateOperation = TableOperation.InsertOrReplace(entity);
            var table = await GetTable(tableName).ConfigureAwait(false);
            await table.ExecuteAsync(updateOperation).ConfigureAwait(false);
        }

        protected async Task DeletePartition(string tableName, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            var table = await GetTable(tableName).ConfigureAwait(false);
            var entities = (await RetrievePartition(tableName, partitionKey).ConfigureAwait(false)).ToArray();
            var deletedRows = 0;
            while (deletedRows < entities.Length)
            {
                var batch = new TableBatchOperation();
                foreach (var entity in entities.Skip(deletedRows).Take(100))
                {
                    batch.Delete(entity);
                }
                var result = await table.ExecuteBatchAsync(batch).ConfigureAwait(false);
                deletedRows += result.Count;
            }
        }
    }
}
