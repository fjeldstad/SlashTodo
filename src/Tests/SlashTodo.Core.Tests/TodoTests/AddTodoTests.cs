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
        public void CanNotAddTodoWithoutId()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => Todo.Add(null, "text", "conversationId", "shortCode", TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(string.Empty, "text", "conversationId", "shortCode", TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add(" ", "text", "conversationId", "shortCode", TodoTestHelpers.GetContext()));
        }

        [Test]
        public void CanNotAddTodoWithEmptyText()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", null, "conversationId", "shortCode", TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", string.Empty, "conversationId", "shortCode", TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", " ", "conversationId", "shortCode", TodoTestHelpers.GetContext()));
        }

        [Test]
        public void CanNotAddTodoWithEmptySlackConversationId()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", "text", null, "shortCode", TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", "text", string.Empty, "shortCode", TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", "text", " ", "shortCode", TodoTestHelpers.GetContext()));
        }

        [Test]
        public void CanNotAddTodoWithEmptyShortCode()
        {
            // Act & assert
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", "text", "conversationId", null, TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", "text", "conversationId", string.Empty, TodoTestHelpers.GetContext()));
            Assert.Throws<ArgumentNullException>(() => Todo.Add("id", "text", "conversationId", " ", TodoTestHelpers.GetContext()));
        }

        [Test]
        public void CanAddTodo()
        {
            // Arrange
            var id = "id";
            var context = TodoTestHelpers.GetContext();
            const string text = " text ";
            var slackConversationId = "slackConversationId";
            var shortCode = "x";
            var before = DateTime.UtcNow;

            // Act
            var todo = Todo.Add(id, text, slackConversationId, shortCode, context);

            // Assert
            Assert.That(todo, Is.Not.Null);
            Assert.That(todo.Id, Is.EqualTo(id));
            Assert.That(todo.Version, Is.EqualTo(1));
            var @event = todo.GetUncommittedEvents().Single() as TodoAdded;
            @event.AssertThatBasicDataIsCorrect(id, slackConversationId, shortCode, context, before, expectedOriginalVersion: 0);
            Assert.That(@event.Text, Is.EqualTo(text.Trim()));
        }
    }
}
