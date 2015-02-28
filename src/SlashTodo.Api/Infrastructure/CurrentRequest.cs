using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;

namespace SlashTodo.Api.Infrastructure
{
    public class CurrentRequest : ICurrentRequest
    {
        private readonly Request _request;

        public Request Request { get { return _request; } }

        public CurrentRequest(NancyContext context)
        {
            _request = context.Request;
        }
    }
}