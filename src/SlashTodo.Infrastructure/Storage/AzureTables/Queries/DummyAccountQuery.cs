using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Queries;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Queries
{
    public class DummyAccountQuery : IAccountQuery
    {
        public Task<Core.Dtos.AccountDto> ById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Core.Dtos.AccountDto> BySlackTeamId(string slackTeamId)
        {
            throw new NotImplementedException();
        }
    }
}
