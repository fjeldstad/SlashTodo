using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;
using SlashTodo.Web.Dtos;

namespace SlashTodo.Web.Queries
{
    public interface IUserQuery
    {
        Task<UserDto> ById(Guid id);
        Task<UserDto> BySlackUserId(string slackUserId);
    }
}
