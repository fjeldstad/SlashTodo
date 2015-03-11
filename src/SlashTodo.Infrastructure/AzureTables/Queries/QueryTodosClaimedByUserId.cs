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
//    public class QueryTodosClaimedByUserId :
//        QueryBase,
//        QueryTodos.IClaimedByUserId
//    {
//        public const string DefaultTableName = "queryTodosClaimedBySlackUserId";

//        public QueryTodosClaimedByUserId(CloudStorageAccount storageAccount)
//            : this(storageAccount, DefaultTableName)
//        {
//        }

//        public QueryTodosClaimedByUserId(CloudStorageAccount storageAccount, string tableName)
//            : base(storageAccount, tableName)
//        {
//        }

//        public Task<TodoDto[]> ClaimedBySlackUserId(string userId)
//        {
//            throw new NotImplementedException();
//        }

//        protected override IEnumerable<ISubscriptionToken> RegisterSubscriptionsCore(ISubscriptionRegistry registry)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
