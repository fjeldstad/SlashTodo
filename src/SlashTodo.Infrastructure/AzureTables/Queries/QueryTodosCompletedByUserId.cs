//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.WindowsAzure.Storage;
//using SlashTodo.Core.Dtos;
//using SlashTodo.Core.Queries;
//using SlashTodo.Infrastructure.AzureTables.Queries.Entities;
//using SlashTodo.Infrastructure.Messaging;

//namespace SlashTodo.Infrastructure.AzureTables.Queries
//{
//    public class QueryTodosCompletedByUserId :
//        QueryBase,
//        QueryTodos.ICompletedByUserId
//    {
//        public const string DefaultTableName = "queryTodosClaimedBySlackUserId";

//        public QueryTodosCompletedByUserId(CloudStorageAccount storageAccount)
//            : this(storageAccount, DefaultTableName)
//        {
//        }

//        public QueryTodosCompletedByUserId(CloudStorageAccount storageAccount, string tableName)
//            : base(storageAccount, tableName)
//        {
//        }

//        public Task<TodoDto[]> CompletedByUserId(string userId, DateTime? since = null, bool includeRemoved = true)
//        {
//            throw new NotImplementedException();
//        }

//        protected override IEnumerable<ISubscriptionToken> RegisterSubscriptionsCore(ISubscriptionRegistry registry)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
