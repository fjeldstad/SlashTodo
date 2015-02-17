using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public class Todo
    {
        private readonly List<ITodoEvent> _uncommittedChanges = new List<ITodoEvent>(); 

        // Internal state
        private readonly TodoContext _context;
        //private string _text;
        //private DateTime _createdAt;
        private string _claimedBy;
        private DateTime? _tickedAt;
        private DateTime? _removedAt;

        public Guid Id { get; protected set; }
        public int Version { get; protected set; }

        public Todo(TodoContext context, IEnumerable<ITodoEvent> historicEvents)
        {
            _context = context;
            LoadFromHistory(historicEvents);
        }

        public Todo(TodoContext context, Guid id, string text)
        {
            _context = context;
            Id = id; // Ugly perhaps, but allows the CreateEvent method to drop a parameter.
            var @event = CreateEvent<TodoAdded>();
            @event.Text = text;
            RaiseEvent(@event);
        }

        public void Claim(bool force = false)
        {
            if (_context.UserId.Equals(_claimedBy, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (_removedAt.HasValue)
            {
                return;
            }
            if (!force)
            {
                ThrowIfClaimedBySomeoneElse();
            }
            RaiseEvent(CreateEvent<TodoClaimed>());
        }

        public void Free(bool force = false)
        {
            if (string.IsNullOrEmpty(_claimedBy))
            {
                return;
            }
            if (!force)
            {
                ThrowIfClaimedBySomeoneElse();
            }
            RaiseEvent(CreateEvent<TodoFreed>());
        }

        public void Tick(bool force = false)
        {
            if (_tickedAt.HasValue)
            {
                return;
            }
            if (_removedAt.HasValue)
            {
                return;
            }
            if (!force)
            {
                ThrowIfClaimedBySomeoneElse();
            }
            RaiseEvent(CreateEvent<TodoTicked>());
        }

        public void Untick()
        {
            if (_removedAt.HasValue)
            {
                return;
            }
            if (!_tickedAt.HasValue)
            {
                return;
            }
            RaiseEvent(CreateEvent<TodoUnticked>());
        }

        public void Remove(bool force = false)
        {
            if (_removedAt.HasValue)
            {
                return;
            }
            if (!force)
            {
                ThrowIfClaimedBySomeoneElse();
            }
            RaiseEvent(CreateEvent<TodoRemoved>());
        }

        public IEnumerable<ITodoEvent> GetUncommittedEvents()
        {
            return _uncommittedChanges.AsEnumerable();
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedChanges.Clear();
        }

        public void LoadFromHistory(IEnumerable<ITodoEvent> historicEvents)
        {
            foreach (var @event in historicEvents)
            {
                ApplyEvent(@event);
            }
        }

        private void ThrowIfClaimedBySomeoneElse()
        {
            if (!string.IsNullOrEmpty(_claimedBy) &&
                !_claimedBy.Equals(_context.UserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new TodoClaimedBySomeoneElseException(_claimedBy);
            }
        }

        private void Apply(TodoAdded @event)
        {
            Id = @event.Id;
            //_createdAt = @event.Timestamp;
            //_text = @event.Text;
        }

        private void Apply(TodoRemoved @event)
        {
            _removedAt = @event.Timestamp;
        }

        private void Apply(TodoClaimed @event)
        {
            _claimedBy = @event.UserId;
        }

        private void Apply(TodoFreed @event)
        {
            _claimedBy = null;
        }

        private void Apply(TodoTicked @event)
        {
            _tickedAt = @event.Timestamp;
            _claimedBy = null;
        }

        private void Apply(TodoUnticked @event)
        {
            _tickedAt = null;
        }

        private void RaiseEvent(ITodoEvent @event)
        {
            @event.OriginalVersion = Version;
            ApplyEvent(@event);
            _uncommittedChanges.Add(@event);
        }

        private void ApplyEvent(ITodoEvent @event)
        {
            if (@event.OriginalVersion != Version)
            {
                throw new TodoEventVersionMismatchException();
            }
            Version++;
            Apply((dynamic)@event);
        }

        private TEvent CreateEvent<TEvent>() 
            where TEvent : ITodoEvent, new()
        {
            if (_context == null)
            {
                throw new InvalidOperationException("Context missing.");
            }
            return new TEvent
            {
                Id = Id,
                Timestamp = DateTime.UtcNow,
                UserId = _context.UserId
            };
        }
    }
}
