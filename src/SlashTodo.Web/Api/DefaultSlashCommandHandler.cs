using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SlashTodo.Core;
using SlashTodo.Infrastructure.Slack;

namespace SlashTodo.Web.Api
{
    public class DefaultSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IRepository<Core.Domain.Todo> _todoRepository;
        private readonly ISlackIncomingWebhookApi _slackIncomingWebhookApi;

        public DefaultSlashCommandHandler(
            IRepository<Core.Domain.Todo> todoRepository,
            ISlackIncomingWebhookApi slackIncomingWebhookApi)
        {
            _todoRepository = todoRepository;
            _slackIncomingWebhookApi = slackIncomingWebhookApi;
        }

        public Task<string> Handle(SlashCommand command)
        {
            return Task.FromResult("*TODO*");
        }
    }
}