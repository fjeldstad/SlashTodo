using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Extensions;

namespace SlashTodo.Web.Logging
{
    public class LoggingErrorHandler : IStatusCodeHandler
    {
        private static readonly HttpStatusCode[] InterestingStatusCodes = new[]
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable
        };
        private readonly ILogger _logger;

        public LoggingErrorHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            var tags = new Dictionary<string, string>();
            tags.Add("statusCode", string.Format("{0} {1}", (int)statusCode, statusCode));
            if (context != null && context.Request != null && context.Request.Url != null)
            {
                tags.Add("url", context.Request.Url);
            }

            Exception exception = null;
            if (context.TryGetException(out exception))
            {
                _logger.LogException(exception, tags: tags);
            }

            // TODO Log non-exceptions?
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return InterestingStatusCodes.Contains(statusCode);
        }
    }
}