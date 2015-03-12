using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Slack;

namespace SlashTodo.Web.Api
{
    public class DefaultTodoMessageFormatter : ITodoMessageFormatter
    {
        public string FormatAsConversationTodoList(IEnumerable<Core.Dtos.TodoDto> todos)
        {
            var existingTodos = todos
                .Where(x => !x.RemovedAt.HasValue)
                .ToArray();
            var shortCodeMaxLength = existingTodos.Max(x => x.ShortCode.Length);
            var incompleteTodos = existingTodos
                .Where(x => !x.CompletedAt.HasValue)
                .OrderBy(x => x.CreatedAt)
                .ToArray();
            var completeTodos = existingTodos
                .Where(x => x.CompletedAt.HasValue)
                .OrderByDescending(x => x.CompletedAt.Value)
                .ToArray();

            var formattedTodoList = new StringBuilder();
            foreach (var todo in incompleteTodos)
            {
                formattedTodoList.AppendFormat("`{0}` {1}", todo.ShortCode.PadLeft(shortCodeMaxLength, ' '), todo.Text);
                if (todo.ClaimedByUserId.HasValue())
                {
                    formattedTodoList.AppendFormat(" `<@{0}>`", todo.ClaimedByUserId);
                }
                formattedTodoList.Append(SlackMessage.NewLine);
            }
            if (completeTodos.Any())
            {
                formattedTodoList.AppendFormat(">*Done:*{0}", SlackMessage.NewLine);
                foreach (var todo in incompleteTodos)
                {
                    formattedTodoList.AppendFormat(">`{0}` {1}{2}", todo.ShortCode.PadLeft(shortCodeMaxLength, ' '), todo.Text, SlackMessage.NewLine);
                }
            }
            return formattedTodoList.ToString();
        }
    }
}