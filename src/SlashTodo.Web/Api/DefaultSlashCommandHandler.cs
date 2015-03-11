using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SlashTodo.Core;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Web.Logging;

namespace SlashTodo.Web.Api
{
    public class DefaultSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IRepository<Core.Domain.Todo> _todoRepository;
        private readonly ISlackIncomingWebhookApi _slackIncomingWebhookApi;
        private readonly ISlashCommandResponseTexts _responseTexts;
        private readonly ILogger _logger;

        public DefaultSlashCommandHandler(
            IRepository<Core.Domain.Todo> todoRepository,
            ISlackIncomingWebhookApi slackIncomingWebhookApi,
            ISlashCommandResponseTexts responseTexts,
            ILogger logger)
        {
            _todoRepository = todoRepository;
            _slackIncomingWebhookApi = slackIncomingWebhookApi;
            _responseTexts = responseTexts;
            _logger = logger;
        }

        public async Task<string> Handle(SlashCommand command, Uri teamIncomingWebhookUrl)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }
            if (teamIncomingWebhookUrl == null)
            {
                throw new ArgumentNullException("teamIncomingWebhookUrl");
            }

            var @operator = command.Text.Words().FirstOrDefault();
            if (@operator.HasValue())
            {
                @operator = @operator.ToLowerInvariant();
            }
            switch (@operator)
            {
                case "help":
                    return _responseTexts.UsageInstructions(command);
                // TODO
                default:
                    return _responseTexts.UnknownCommand(command);
            }

            await _slackIncomingWebhookApi.Send(teamIncomingWebhookUrl, new SlackIncomingWebhookMessage
            {
                UserName = "/todo",
                ConversationId = command.ConversationId,
                Text = string.Format("*Echo:* {0} {1}", command.Command, command.Text)
            }).ConfigureAwait(false);
        }
    }
}