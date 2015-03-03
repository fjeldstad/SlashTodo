using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Testing;

namespace SlashTodo.Web.Tests
{
    public static class BrowserResponseExtensions
    {
        public static void ShouldHaveRedirectedTo(this BrowserResponse response, Predicate<string> locationPredicate)
        {
            if (!new HttpStatusCode[3]
            {
                HttpStatusCode.MovedPermanently,
                HttpStatusCode.SeeOther,
                HttpStatusCode.TemporaryRedirect
            }.Any((x => x == response.StatusCode)))
            {
                throw new AssertException(string.Format("Status code should be one of 'MovedPermanently, SeeOther, TemporaryRedirect', but was {0}.", response.StatusCode));
            }

            var location = response.Headers["Location"];
            if (!locationPredicate(location))
            {
                throw new AssertException(string.Format("Location does not match the predicate; was: {0}", location));
            }
        }
    }
}
