using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Core.Queries
{
    public class QueryUsers
    {
        public interface IById
        {
            Task<UserDto> ById(string id);
        }
    }
}
