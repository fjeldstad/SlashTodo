using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.TodoTests
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
            const string text = " text ";
            var before = DateTime.UtcNow;

            // Act
            var todo = Todo.Add(id, context, text);

            // Assert
            Assert.That(todo, Is.Not.Null);
            Assert.That(todo.Id, Is.EqualTo(id));
            Assert.That(todo.Version, Is.EqualTo(1));
            var @event = todo.GetUncommittedEvents().Single() as TodoAdded;
            @event.AssertThatBasicDataIsCorrect(id, context, before, expectedOriginalVersion: 0);
            Assert.That(@event.Text, Is.EqualTo(text.Trim()));
        }
    }
}
