using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public abstract class Aggregate : IEquatable<Aggregate>
    {
        private readonly List<IDomainEvent> _uncommittedEvents = new List<IDomainEvent>();

        public string Id { get; protected set; }
        public int Version { get; protected set; }

        protected abstract void ApplyEventCore(IDomainEvent @event);

        public IEnumerable<IDomainEvent> GetUncommittedEvents()
        {
            return _uncommittedEvents.AsEnumerable();
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        public void LoadFromEvents(IEnumerable<IDomainEvent> events)
        {
            if (Version > 0)
            {
                throw new InvalidOperationException("LoadFromEvents can only be used with the initial version of the aggregate.");
            }
            foreach (var @event in events)
            {
                ApplyEvent(@event);
            }
        }

        protected virtual void RaiseEvent(IDomainEvent @event)
        {
            @event.Id = Id;
            @event.Timestamp = DateTime.UtcNow;
            @event.OriginalVersion = Version;
            ApplyEvent(@event);
            _uncommittedEvents.Add(@event);
        }

        protected void ApplyEvent(IDomainEvent @event)
        {
            ApplyEventCore(@event);
            Version++;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Aggregate);
        }

        public virtual bool Equals(Aggregate other)
        {
            return other != null && string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
        }
    }
}
