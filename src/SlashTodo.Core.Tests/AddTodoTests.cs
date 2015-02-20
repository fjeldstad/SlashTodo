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
    public class AddTodoTests
    {
        [Test]
        public void CanNotAddTodoWithEmptyText()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), It.IsAny<TodoContext>(), null));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), It.IsAny<TodoContext>(), string.Empty));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), It.IsAny<TodoContext>(), " "));
        }

        [Test]
        public void CanAddTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var text = " text ";

            // Act
            var todo = Todo.Add(id, context, text);

            // Assert
            Assert.That(todo, Is.Not.Null);
            Assert.That(todo.Id, Is.EqualTo(id));
            var data = todo.GetData();
            Assert.That(data.ClaimedBy, Is.Null);
            Assert.That(data.Text, Is.EqualTo(text.Trim()));
        }

        [Test]
        public void AddTodoProducesPendingTodoAddedEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TestHelpers.GetContext();
            var text = " text ";
            var before = DateTime.UtcNow;

            // Act
            var todo = Todo.Add(id, context, text);

            // Assert
            var @event = todo.GetPendingEvents().Single() as TodoAdded;
            Assert.That(@event, Is.Not.Null);
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.UserId, Is.EqualTo(context.UserId));
            Assert.That(@event.Timestamp, Is.InRange(before, DateTime.UtcNow));
            Assert.That(@event.Text, Is.EqualTo(text.Trim()));
        }
    }
}
