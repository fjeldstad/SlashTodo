using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;
using SlashTodo.Web.Dtos;

namespace SlashTodo.Web.Queries
{
    public interface IAccountQuery
    {
        Task<AccountDto> ById(Guid id);
        Task<AccountDto> BySlackTeamId(string slackTeamId);
    }
}
