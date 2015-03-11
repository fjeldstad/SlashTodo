using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SlashTodo.Infrastructure.Tests
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        public void WordsReturnsEmptyWhenInputIsNull()
        {
            // Arrange
            string input = null;

            // Act
            var words = input.Words();

            // Assert
            Assert.That(words, Is.Empty);
        }

        [Test]
        public void WordsReturnsEmptyWhenInputIsWhitespace()
        {
            // Arrange
            string input = "       ";

            // Act
            var words = input.Words();

            // Assert
            Assert.That(words, Is.Empty);
        }

        [Test]
        public void WordsReturnsExpectedValues()
        {
            // Arrange
            string input = " one  two   three ";

            // Act
            var words = input.Words();

            // Assert
            Assert.That(words.SequenceEqual(new [] { "one", "two", "three" }));
        }
    }
}
