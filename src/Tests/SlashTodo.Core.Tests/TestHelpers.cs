using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core.Tests
{
    public static class TestHelpers
    {
        public static void AssertThrows<TException>(Action action, Predicate<TException> predicate) where TException : Exception
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
    }
}
