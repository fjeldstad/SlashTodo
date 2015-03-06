using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests
{
    [TestFixture]
    public class RepositoryTests
    {
        private Mock<IMessageBus> _bus;
        private Mock<IEventStore> _eventStore;
        private Repository<DummyAggregate> _repository;

        [SetUp]
        public void BeforeEachTest()
        {
            _eventStore = new Mock<IEventStore>();
            _bus = new Mock<IMessageBus>();
            _bus.Setup(x => x.Publish(It.IsAny<IMessage>())).Returns(Task.FromResult((object)null));
            _repository = new DummyRepository(_eventStore.Object, _bus.Object);
        }

        [Test]
        public async Task CanBuildAggregateFromEvents()
        {
            // Arrange
            var id = Guid.NewGuid();
            var events = new[]
            {
                new Mock<IDomainEvent>().Object,
                new Mock<IDomainEvent>().Object,
                new Mock<IDomainEvent>().Object
            };
            _eventStore.Setup(x => x.GetById(id)).Returns(Task.FromResult<IEnumerable<IDomainEvent>>(events));

            // Act
            var aggregate = await _repository.GetById(id);

            // Assert
            var appliedEvents = aggregate.GetAppliedEvents().ToArray();
            Assert.That(appliedEvents.Length, Is.EqualTo(events.Length));
            Assert.That(events.All(appliedEvents.Contains));
        }

        [Test]
        public async Task AggregateBuiltFromHistoricEventsHasNoUncommittedEvents()
        {
            // Arrange
            var id = Guid.NewGuid();
            var events = new[]
            {
                new Mock<IDomainEvent>().Object,
                new Mock<IDomainEvent>().Object,
                new Mock<IDomainEvent>().Object
            };
            _eventStore.Setup(x => x.GetById(id)).Returns(Task.FromResult<IEnumerable<IDomainEvent>>(events));

            // Act
            var aggregate = await _repository.GetById(id);

            // Assert
            Assert.That(aggregate.GetUncommittedEvents(), Is.Empty);
        }

        [Test]
        public async Task RepositoryReturnsNullWhenNoEventsExistForAggregateId()
        {
            // Arrange
            var id = Guid.NewGuid();
            _eventStore.Setup(x => x.GetById(id)).Returns(Task.FromResult<IEnumerable<IDomainEvent>>(new IDomainEvent[0]));

            // Act
            var aggregate = await _repository.GetById(id);

            // Assert
            Assert.That(aggregate, Is.Null);
        }

        [Test]
        public async Task SaveCallsEventStoreSaveWithCorrectArguments()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new DummyAggregate(id);
            var expectedVersion = aggregate.Version;
            aggregate.DoSomething();
            aggregate.DoSomething();
            aggregate.DoSomething();
            var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();
            Assert.That(uncommittedEvents, Is.Not.Empty);

            // Act
            await _repository.Save(aggregate);

            // Assert
            _eventStore.Verify(x => x.Save(
                It.Is<Guid>(a => a == id),
                expectedVersion,
                It.Is<IEnumerable<IDomainEvent>>(events =>
                    events.Count() == uncommittedEvents.Length &&
                    uncommittedEvents.All(e => events.Contains(e)))), Times.Once);
        }

        [Test]
        public async Task SavePublishesEventsToMessageBus()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new DummyAggregate(id);
            aggregate.DoSomething();
            aggregate.DoSomething();
            aggregate.DoSomething();
            var publishedMessages = new List<IMessage>();
            _bus.Setup(x => x.Publish(It.IsAny<IMessage>())).Callback((IMessage m) => publishedMessages.Add(m)).Returns(Task.FromResult<object>(null));
            var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();
            Assert.That(uncommittedEvents, Is.Not.Empty);

            // Act
            await _repository.Save(aggregate);

            // Assert
            Assert.That(uncommittedEvents.SequenceEqual(publishedMessages));
        }

        [Test]
        public async Task SaveClearsUncommittedEvents()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new DummyAggregate(id);
            var expectedVersion = aggregate.Version;
            aggregate.DoSomething();
            aggregate.DoSomething();
            aggregate.DoSomething();
            var uncommittedEvents = aggregate.GetUncommittedEvents().ToArray();
            Assert.That(uncommittedEvents, Is.Not.Empty);

            // Act
            await _repository.Save(aggregate);

            // Assert
            Assert.That(aggregate.GetUncommittedEvents(), Is.Empty);
        }

        [Test]
        public async Task SaveDoesNothingWhenThereAreNoUncommittedEvents()
        {
            // Arrange
            var aggregate = new DummyAggregate();
            Assert.That(aggregate.GetUncommittedEvents(), Is.Empty);

            // Act
            await _repository.Save(aggregate);

            // Assert
            _eventStore.Verify(x => x.Save(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<IEnumerable<IDomainEvent>>()), Times.Never);
        }

        public class DummyRepository : Repository<DummyAggregate>
        {
            public DummyRepository(IEventStore eventStore, IMessageBus bus) : base(eventStore, bus)
            {
            }
        }

        public class DummyAggregate : Aggregate
        {
            private readonly List<IDomainEvent> _appliedEvents = new List<IDomainEvent>();

            public DummyAggregate()
                : this(Guid.NewGuid())
            {
            }

            public DummyAggregate(Guid id)
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
                RaiseEvent(new Mock<IDomainEvent>().Object);
            }
        }
    }
}
