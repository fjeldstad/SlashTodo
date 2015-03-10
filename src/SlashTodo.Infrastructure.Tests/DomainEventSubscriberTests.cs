using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Infrastructure.Messaging;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests
{
    [TestFixture]
    public class DomainEventSubscriberTests
    {
        [Test]
        public async Task SubscriberReceivesDomainEventPublishedByRepository()
        {
            // Arrange
            var aggregate = new DummyAggregate(id: "id");
            var eventStore = new Mock<IEventStore>();
            eventStore.Setup(x => x.Save(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IEnumerable<IDomainEvent>>())).Returns(Task.FromResult<object>(null));
            var bus = new TinyMessageBus(new TinyMessengerHub());
            var repository = new DummyRepository(eventStore.Object, bus);
            var subscriber = new DummySubscriber();
            subscriber.RegisterSubscriptions(bus);
            aggregate.DoSomething();
            Assert.That(!subscriber.ReceivedDomainEvents.Any());
            Assert.That(aggregate.GetUncommittedEvents().Any());
            
            // Act
            await repository.Save(aggregate).ConfigureAwait(false);

            // Assert
            Assert.That(subscriber.ReceivedDomainEvents.Any());
        }

        public class DummySubscriber : ISubscriber
        {
            private readonly List<ISubscriptionToken> _tokens = new List<ISubscriptionToken>();
            private readonly List<IDomainEvent> _receivedDomainEvents = new List<IDomainEvent>();

            public IEnumerable<IDomainEvent> ReceivedDomainEvents { get { return _receivedDomainEvents.AsEnumerable(); } } 
 
            public void RegisterSubscriptions(ISubscriptionRegistry registry)
            {
                registry.RegisterSubscription<DummyEvent>(@event => _receivedDomainEvents.Add(@event));
            }

            public void Dispose()
            {
                foreach (var token in _tokens)
                {
                    token.Dispose();
                }
            }
        }

        public class DummyRepository : Repository<DummyAggregate>
        {
            public DummyRepository(IEventStore eventStore, IEventDispatcher eventDispatcher)
                : base(eventStore, eventDispatcher)
            {
            }
        }

        public class DummyAggregate : Aggregate
        {
            private readonly List<IDomainEvent> _appliedEvents = new List<IDomainEvent>();

            public DummyAggregate()
                : this("id")
            {
            }

            public DummyAggregate(string id)
            {
                Id = id;
            }

            public IEnumerable<IDomainEvent> GetAppliedEvents()
            {
                return _appliedEvents.AsEnumerable();
            }

            protected override void ApplyEventCore(IDomainEvent @event)
            {
                _appliedEvents.Add(@event);
            }

            public void DoSomething()
            {
                RaiseEvent(new DummyEvent());
            }
        }

        public class DummyEvent : IDomainEvent
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public int OriginalVersion { get; set; }
        }
    }
}
