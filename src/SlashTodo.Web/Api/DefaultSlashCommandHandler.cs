using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using SlashTodo.Core;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Web.Logging;

namespace SlashTodo.Web.Api
{
    public class DefaultSlashCommandHandler : ISlashCommandHandler
    {
        private readonly IRepository<Core.Domain.Todo> _todoRepository;
        private readonly QueryTodos.IBySlackConversationId _queryTodosBySlackConversationId;
        private readonly ISlackIncomingWebhookApi _slackIncomingWebhookApi;
        private readonly ISlashCommandResponseTexts _responseTexts;
        private readonly ITodoMessageFormatter _todoFormatter;
        private readonly ILogger _logger;

        public DefaultSlashCommandHandler(
            IRepository<Core.Domain.Todo> todoRepository,
            QueryTodos.IBySlackConversationId queryTodosBySlackConversationId,
            ISlackIncomingWebhookApi slackIncomingWebhookApi,
            ISlashCommandResponseTexts responseTexts,
            ITodoMessageFormatter todoFormatter,
            ILogger logger)
        {
            _todoRepository = todoRepository;
            _queryTodosBySlackConversationId = queryTodosBySlackConversationId;
            _slackIncomingWebhookApi = slackIncomingWebhookApi;
            _responseTexts = responseTexts;
            _todoFormatter = todoFormatter;
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
            try
            {
                switch (@operator)
                {
                    case "help":
                        return _responseTexts.UsageInstructions(command);
                    case null:
                    case "":
                    {
                        var todos = await _queryTodosBySlackConversationId.BySlackConversationId(command.ConversationId).ConfigureAwait(false);
                        return _todoFormatter.FormatAsConversationTodoList(todos);
                    }
                    case "show":
                    {
                        var todos = await _queryTodosBySlackConversationId.BySlackConversationId(command.ConversationId).ConfigureAwait(false);
                        var formattedText = _todoFormatter.FormatAsConversationTodoList(todos);
                        await SendToIncomingWebhook(teamIncomingWebhookUrl, command.ConversationId, formattedText).ConfigureAwait(false);
                        return null;
                    }
                        // TODO
                    default:
                        return _responseTexts.UnknownCommand(command);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogException(ex);
                return _responseTexts.ErrorSendingToIncomingWebhook();
            }

        }

        private async Task SendToIncomingWebhook(Uri incomingWebhookUrl, string conversationId, string text)
        {
            await _slackIncomingWebhookApi.Send(incomingWebhookUrl, new SlackIncomingWebhookMessage
            {
                UserName = "/todo",
                IconEmoji = ":white_check_mark:",
                ConversationId = conversationId,
                Text = text,
                EnableMarkdown = true
            }).ConfigureAwait(false);
        }
    }
}