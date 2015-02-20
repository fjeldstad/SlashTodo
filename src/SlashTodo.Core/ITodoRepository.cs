using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public interface ITodoRepository
    {
        Todo GetById(Guid id);
        void Save(Todo todo);
    }
}
