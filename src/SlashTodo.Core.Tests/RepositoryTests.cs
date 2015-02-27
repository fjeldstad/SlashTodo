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
        private Mock<IEventStore> _eventStore;
        private Repository<DummyAggregate> _repository;

        [SetUp]
        public void BeforeEachTest()
        {
            _eventStore = new Mock<IEventStore>();
            _repository = new Repository<DummyAggregate>(_eventStore.Object);
        }

        [Test]
        public void CanBuildAggregateFromEvents()
        {
            // Arrange
            var id = Guid.NewGuid();
            var events = new[]
            {
                new Mock<IDomainEvent>().Object,
                new Mock<IDomainEvent>().Object,
                new Mock<IDomainEvent>().Object
            };
            _eventStore.Setup(x => x.GetByAggregateId(id)).Returns(events);

            // Act
            var aggregate = _repository.GetById(id);

            // Assert
            var appliedEvents = aggregate.GetAppliedEvents().ToArray();
            Assert.That(appliedEvents.Length, Is.EqualTo(events.Length));
            Assert.That(events.All(appliedEvents.Contains));
        }

        [Test]
        public void RepositoryReturnsNullWhenNoEventsExistForAggregateId()
        {
            // Arrange
            var id = Guid.NewGuid();
            _eventStore.Setup(x => x.GetByAggregateId(id)).Returns(new IDomainEvent[0]);

            // Act
            var aggregate = _repository.GetById(id);

            // Assert
            Assert.That(aggregate, Is.Null);
        }

        [Test]
        public void SaveCallsEventStoreSaveWithCorrectArguments()
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
            _repository.Save(aggregate);

            // Assert
            _eventStore.Verify(x => x.Save(
                It.Is<Guid>(a => a == id),
                expectedVersion,
                It.Is<IEnumerable<IDomainEvent>>(events =>
                    events.Count() == uncommittedEvents.Length &&
                    uncommittedEvents.All(e => events.Contains(e)))), Times.Once);
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
