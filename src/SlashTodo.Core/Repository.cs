using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public class Repository<T> where T : Aggregate, new()
    {
        private readonly IEventStore _eventStore;

        public Repository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        } 

        public T GetById(Guid id)
        {
            var events = _eventStore.GetByAggregateId(id).ToArray();
            if (!events.Any())
            {
                return null;
            }
            var aggregate = new T();
            aggregate.LoadFromEvents(events);
            return aggregate;
        }

        public void Save(T aggregate)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException("aggregate");
            }
            var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();
            if (uncommittedEvents.Any())
            {
                _eventStore.Save(aggregate.Id, uncommittedEvents.First().OriginalVersion, uncommittedEvents);
            }
        }
    }
}
