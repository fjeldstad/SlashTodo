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
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), null, It.IsAny<string>(), It.IsAny<TodoContext>()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), string.Empty, It.IsAny<string>(), It.IsAny<TodoContext>()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), " ", It.IsAny<string>(), It.IsAny<TodoContext>()));
        }

        [Test]
        public void CanNotAddTodoWithEmptySlackConversationId()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), It.IsAny<string>(), null, It.IsAny<TodoContext>()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), It.IsAny<string>(), string.Empty, It.IsAny<TodoContext>()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(It.IsAny<Guid>(), It.IsAny<string>(), " ", It.IsAny<TodoContext>()));
        }

        [Test]
        public void CanAddTodo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var context = TodoTestHelpers.GetContext();
            const string text = " text ";
            var slackConversationId = "slackConversationId";
            var before = DateTime.UtcNow;

            // Act
            var todo = Todo.Add(id, text, slackConversationId, context);

            // Assert
            Assert.That(todo, Is.Not.Null);
            Assert.That(todo.Id, Is.EqualTo(id));
            Assert.That(todo.Version, Is.EqualTo(1));
            var @event = todo.GetUncommittedEvents().Single() as TodoAdded;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, context, before, expectedOriginalVersion: 0);
            Assert.That(@event.Text, Is.EqualTo(text.Trim()));
        }
    }
}
