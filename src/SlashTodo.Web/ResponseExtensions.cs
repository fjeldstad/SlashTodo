using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;

namespace SlashTodo.Web
{
    public static class ResponseExtensions
    {
        public static Response WithReasonPhrase(this HttpStatusCode statusCode, string reasonPhrase)
        {
            return new Response
            {
                StatusCode = statusCode,
                ReasonPhrase = reasonPhrase
            };
        }
    }
}