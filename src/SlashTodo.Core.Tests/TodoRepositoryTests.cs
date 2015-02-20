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
    public class TodoRepositoryTests
    {
        private Mock<ITodoStore> _store;
        private Mock<ITodoEventDispatcher> _dispatcher;
        private TodoContext _context;
        private TodoRepository _repo;

        [SetUp]
        public void BeforeEachTest()
        {
            _store = new Mock<ITodoStore>();
            _dispatcher = new Mock<ITodoEventDispatcher>();
            _context = TestHelpers.GetContext();
            _repo = new TodoRepository(_store.Object, _dispatcher.Object, _context);
        }

        [Test]
        public void ConstructorThrowsOnMissingArguments()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => new TodoRepository(null, _dispatcher.Object, TestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => new TodoRepository(_store.Object, null, TestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => new TodoRepository(_store.Object, _dispatcher.Object, null));
        }

        [Test]
        public void GetByIdReturnsNullWhenStoreDoesNotContainRequestedData()
        {
            // Arrange
            var id = Guid.NewGuid();
            _store.Setup(x => x.Read(id)).Returns((TodoData)null);

            // Act
            var todo = _repo.GetById(id);

            // Assert
            Assert.That(todo, Is.Null);
        }

        [Test]
        public void GetByIdUsesDataFromStore()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedData = TestHelpers.GetData();
            _store.Setup(x => x.Read(id)).Returns(expectedData);

            // Act
            var todo = _repo.GetById(id);

            // Assert
            var actualData = todo.GetData();
            _store.Verify(x => x.Read(id), Times.Once);
            Assert.That(actualData.State, Is.EqualTo(expectedData.State));
            Assert.That(actualData.Text, Is.EqualTo(expectedData.Text));
            Assert.That(actualData.ClaimedBy, Is.EqualTo(expectedData.ClaimedBy));
        }

        [Test]
        public void SaveThrowsOnMissingArguments()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => _repo.Save(null));
        }

        [Test]
        public void SaveWritesDataToStore()
        {
            // Arrange
            var todo = TestHelpers.GetTodo();
            var data = todo.GetData();

            // Act
            _repo.Save(todo);

            // Assert
            _store.Verify(x => x.Write(todo.Id, It.Is<TodoData>(d =>
                d.State == data.State &&
                d.Text == data.Text &&
                d.ClaimedBy == data.ClaimedBy)), Times.Once);
        }

        [Test]
        public void SaveProcessesPendingEvents()
        {
            // Arrange
            var todo = Todo.Add(Guid.NewGuid(), _context, "text");
            todo.Claim();
            todo.Tick();
            var pendingEvents = todo.GetPendingEvents().ToArray();
            Assert.That(pendingEvents, Is.Not.Empty);
             

            // Act
            _repo.Save(todo);

            // Assert
            foreach (var pendingEvent in pendingEvents)
            {
                _dispatcher.Verify(x => x.Publish(pendingEvent), Times.Once);
            }
            Assert.That(todo.GetPendingEvents(), Is.Empty);
        }
    }
}
