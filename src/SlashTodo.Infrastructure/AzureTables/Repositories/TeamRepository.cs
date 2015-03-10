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
    public class TeamRepository : Repository<Core.Domain.Team>
    {
        public const string DefaultTableName = "teams";

        public TeamRepository(CloudStorageAccount storageAccount, IEventDispatcher eventDispatcher)
            : this(storageAccount, eventDispatcher, DefaultTableName)
        {
        }

        public TeamRepository(CloudStorageAccount storageAccount, IEventDispatcher eventDispatcher, string tableName)
            : base(new EventStore(storageAccount, tableName), eventDispatcher)
        {
        }
    }
}
