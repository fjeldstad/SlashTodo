using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Tests.Common
{
    public static class CustomAssert
    {
        public static void Throws<TException>(Action action, Predicate<TException> predicate) where TException : Exception
        {
            try
            {
                action();
                Assert.Fail("Expected an exception of type {0} to be thrown.", typeof(TException).Name);
            }
            catch (TException ex)
            {
                if (!predicate(ex))
                {
                    Assert.Fail("The exception does not fulfil the expected criteria.");
                }
            }
        }

        public static void AssertThatBasicEventDataIsCorrect(
            this IDomainEvent @event,
            Guid id,
            DateTime earliestExpectedTimestamp,
            int? expectedOriginalVersion = null)
        {
            Assert.That(@event, Is.Not.Null);
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.Timestamp, Is.InRange(earliestExpectedTimestamp, DateTime.UtcNow));
            if (expectedOriginalVersion.HasValue)
            {
                Assert.That(@event.OriginalVersion, Is.EqualTo(expectedOriginalVersion.Value));
            }
        }
    }
}
