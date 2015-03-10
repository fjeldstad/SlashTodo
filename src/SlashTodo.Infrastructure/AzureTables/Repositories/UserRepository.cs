using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SlashTodo.Core;

namespace SlashTodo.Infrastructure.AzureTables.Repositories
{
    public class UserRepository : Repository<Core.Domain.User>
    {
        public const string DefaultTableName = "users";

        public UserRepository(CloudStorageAccount storageAccount, IEventDispatcher eventDispatcher)
            : this(storageAccount, eventDispatcher, DefaultTableName)
        {
        }

        public UserRepository(CloudStorageAccount storageAccount, IEventDispatcher eventDispatcher, string tableName)
            : base(new EventStore(storageAccount, tableName), eventDispatcher)
        {
        }
    }
}
