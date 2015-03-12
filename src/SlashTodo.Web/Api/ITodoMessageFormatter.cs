using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Web.Api
{
    public interface ITodoMessageFormatter
    {
        string FormatAsConversationTodoList(IEnumerable<TodoDto> todos);
    }
}
