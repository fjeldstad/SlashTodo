using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Tests.Todo;

namespace SlashTodo.Core.Tests
{
    [TestFixture]
    public class AddTests
    {
        [Test]
        public void CanAddTodo()
        {
            // Arrange
            var context = Common.GetContext();
            var id = Guid.NewGuid();
            var text = "text";
            var before = DateTime.UtcNow;

            // Act
            var todo = new Core.Todo(context, id, text);

            // Assert
            var todoAdded = todo.GetUncommittedEvents().SingleOrDefault() as TodoAdded;
            Assert.IsNotNull(todoAdded);
            Assert.That(todoAdded.Id, Is.EqualTo(id));
            Assert.That(todoAdded.UserId, Is.EqualTo(context.UserId));
            Assert.That(todoAdded.OriginalVersion, Is.EqualTo(0));
            Assert.That(todoAdded.Timestamp, Is.InRange(before, DateTime.UtcNow));
            Assert.That(todoAdded.Text, Is.EqualTo(text));
        }
    }
}
