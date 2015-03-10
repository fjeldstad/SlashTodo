using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace SlashTodo.Infrastructure.AzureTables
{
    public static class Helpers
    {
        public static async Task<CloudTable> GetTableAsync(this CloudStorageAccount storageAccount, string tableName, bool createIfNotExists = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            if (createIfNotExists)
            {
                await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
            return table;
        }

        public static CloudTable GetTable(this CloudStorageAccount storageAccount, string tableName, bool createIfNotExists = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            if (createIfNotExists)
            {
                table.CreateIfNotExists();
            }
            return table;
        }

        public static async Task InsertAsync<TEntity>(this CloudTable table, TEntity entity)
            where TEntity : class, ITableEntity, new()
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }

        public static void Insert<TEntity>(this CloudTable table, TEntity entity)
            where TEntity : class, ITableEntity, new()
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var insertOperation = TableOperation.Insert(entity);
            table.Execute(insertOperation);
        }

        public static async Task InsertBatchAsync<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : class, ITableEntity, new()
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
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

        public static void InsertBatch<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : class, ITableEntity, new()
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }
            var entityArray = entities.ToArray();
            var insertedRows = 0;
            while (insertedRows < entityArray.Length)
            {
                var batch = new TableBatchOperation();
                foreach (var entity in entityArray.Skip(insertedRows).Take(100))
                {
                    batch.Insert(entity);
                }
                var result = table.ExecuteBatch(batch);
                insertedRows += result.Count;
            }
        }

        public static async Task<TEntity> RetrieveAsync<TEntity>(this CloudTable table, string partitionKey, string rowKey)
            where TEntity : class, ITableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException("rowKey");
            }
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            return (await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false)).Result as TEntity;
        }

        public static TEntity Retrieve<TEntity>(this CloudTable table, string partitionKey, string rowKey)
            where TEntity : class, ITableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException("rowKey");
            }
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            return table.Execute(retrieveOperation).Result as TEntity;
        }

        public static async Task<IEnumerable<TEntity>> RetrievePartitionAsync<TEntity>(this CloudTable table, string partitionKey)
            where TEntity : class, ITableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            var query = new TableQuery<TEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey",
                    QueryComparisons.Equal,
                    partitionKey));

            var entities = new List<TEntity>();
            TableQuerySegment<TEntity> currentSegment = null;
            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                currentSegment = await table.ExecuteQuerySegmentedAsync(
                    query,
                    currentSegment != null ? currentSegment.ContinuationToken : null).ConfigureAwait(false);
                entities.AddRange(currentSegment.Results);
            }
            return entities;
        }

        public static IEnumerable<TEntity> RetrievePartition<TEntity>(this CloudTable table, string partitionKey)
            where TEntity : class, ITableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            var query = new TableQuery<TEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey",
                    QueryComparisons.Equal,
                    partitionKey));

            var entities = new List<TEntity>();
            TableQuerySegment<TEntity> currentSegment = null;
            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                currentSegment = table.ExecuteQuerySegmented(
                    query,
                    currentSegment != null ? currentSegment.ContinuationToken : null);
                entities.AddRange(currentSegment.Results);
            }
            return entities;
        }

        public static async Task UpdateAsync<TEntity>(this CloudTable table, TEntity entity)
            where TEntity : class, ITableEntity, new()
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var updateOperation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(updateOperation).ConfigureAwait(false);
        }

        public static void Update<TEntity>(this CloudTable table, TEntity entity)
            where TEntity : class, ITableEntity, new()
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var updateOperation = TableOperation.InsertOrReplace(entity);
            table.Execute(updateOperation);
        }

        public static async Task DeleteAsync(this CloudTable table, string partitionKey, string rowKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException("rowKey");
            }
            var entity = await table.RetrieveAsync<TableEntity>(partitionKey, rowKey).ConfigureAwait(false);
            if (entity != null)
            {
                var deleteOperation = TableOperation.Delete(entity);
                await table.ExecuteAsync(deleteOperation).ConfigureAwait(false);
            }
        }

        public static void Delete(this CloudTable table, string partitionKey, string rowKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException("rowKey");
            }
            var entity = table.Retrieve<TableEntity>(partitionKey, rowKey);
            if (entity != null)
            {
                var deleteOperation = TableOperation.Delete(entity);
                table.Execute(deleteOperation);
            }
        }

        public static async Task DeletePartitionAsync(this CloudTable table, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            var entities = (await table.RetrievePartitionAsync<TableEntity>(partitionKey).ConfigureAwait(false)).ToArray();
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

        public static void DeletePartition(this CloudTable table, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException("partitionKey");
            }
            var entities = table.RetrievePartition<TableEntity>(partitionKey).ToArray();
            var deletedRows = 0;
            while (deletedRows < entities.Length)
            {
                var batch = new TableBatchOperation();
                foreach (var entity in entities.Skip(deletedRows).Take(100))
                {
                    batch.Delete(entity);
                }
                var result = table.ExecuteBatch(batch);
                deletedRows += result.Count;
            }
        }
    }
}
