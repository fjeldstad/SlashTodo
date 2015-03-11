using System;
using System.Collections.Generic;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Extensions;

namespace SlashTodo.Web.Logging
{
    public class LoggingErrorHandler : IStatusCodeHandler
    {
        private readonly ILogger _logger;

        public LoggingErrorHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            Exception exception = null;
            if (context.TryGetException(out exception))
            {
                _logger.LogException(exception, tags: new Dictionary<string, string>{{ "statusCode", statusCode.ToString() }});
            }
            else
            {
                // TODO Well...
            }
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.InternalServerError;
        }
    }
}