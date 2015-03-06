using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Queries;

namespace SlashTodo.Infrastructure.Storage.AzureTables.Queries
{
    public class DummyUserQuery : IUserQuery
    {
        public Task<Core.Dtos.UserDto> ById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Core.Dtos.UserDto> BySlackUserId(string slackUserId)
        {
            throw new NotImplementedException();
        }
    }
}
