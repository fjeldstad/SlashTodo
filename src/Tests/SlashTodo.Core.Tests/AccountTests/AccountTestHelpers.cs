using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests.AccountTests
{
    public static class AccountTestHelpers
    {
        public static void AssertThatBasicDataIsCorrect(
            this AccountEvent @event, 
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
