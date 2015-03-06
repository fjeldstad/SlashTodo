using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public class Repository<T> : IRepository<T> where T : Aggregate, new()
    {
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _bus;

        protected Repository(IEventStore eventStore, IMessageBus bus)
        {
            _eventStore = eventStore;
            _bus = bus;
        } 

        public async Task<T> GetById(Guid id)
        {
            var events = (await _eventStore.GetById(id).ConfigureAwait(false)).ToArray();
            if (!events.Any())
            {
                return null;
            }
            var aggregate = new T();
            aggregate.LoadFromEvents(events);
            return aggregate;
        }

        public async Task Save(T aggregate)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException("aggregate");
            }
            var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();
            if (uncommittedEvents.Any())
            {
                await _eventStore.Save(aggregate.Id, uncommittedEvents.First().OriginalVersion, uncommittedEvents).ConfigureAwait(false);
                foreach (var @event in uncommittedEvents)
                {
                    await _bus.Publish(@event).ConfigureAwait(false);
                }
                aggregate.ClearUncommittedEvents();
            }
        }
    }
}
