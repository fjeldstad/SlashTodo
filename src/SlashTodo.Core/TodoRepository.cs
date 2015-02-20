using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public class TodoRepository : ITodoRepository
    {
        private readonly ITodoStore _store;
        private readonly ITodoEventDispatcher _dispatcher;
        private readonly TodoContext _context;

        public TodoRepository(
            ITodoStore store,
            ITodoEventDispatcher dispatcher,
            TodoContext context)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            _store = store;
            _dispatcher = dispatcher;
            _context = context;
        }

        public Todo GetById(Guid id)
        {
            var data = _store.Read(id);
            if (data == null)
            {
                return null;
            }
            var todo = new Todo(id, _context, data);
            return todo;
        }

        public void Save(Todo todo)
        {
            if (todo == null)
            {
                throw new ArgumentNullException("todo");
            }
            _store.Write(todo.Id, todo.GetData());
            foreach (var pendingEvent in todo.GetPendingEvents())
            {
                _dispatcher.Publish(pendingEvent);
            }
            todo.ClearPendingEvents();
        }
    }
}
