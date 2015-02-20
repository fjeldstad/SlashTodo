using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public interface ITodoStore
    {
        TodoData Read(Guid id);
        void Write(Guid id, TodoData data);
    }
}
