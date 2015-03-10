using System;
using Microsoft.WindowsAzure.Storage.Table;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Infrastructure.AzureTables.Queries.Entities
{
    public class TodoDtoTableEntity : TableEntity
    {
        public string Id { get; set; }
        public string TeamId { get; set; }
        public string SlackConversationId { get; set; }
        public string ShortCode { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? RemovedAt { get; set; }
        public string ClaimedByUserId { get; set; }

        public TodoDtoTableEntity()
        {
        }

        public TodoDtoTableEntity(TodoDto dto, Func<TodoDto, string> partitionKey, Func<TodoDto, string> rowKey)
        {
            PartitionKey = partitionKey(dto);
            RowKey = rowKey(dto);

            Id = dto.Id;
            TeamId = dto.TeamId;
            SlackConversationId = dto.SlackConversationId;
            ShortCode = dto.ShortCode;
            Text = dto.Text;
            CreatedAt = dto.CreatedAt;
            CompletedAt = dto.CompletedAt;
            RemovedAt = dto.RemovedAt;
            ClaimedByUserId = dto.ClaimedByUserId;
        }

        public TodoDto GetTodoDto()
        {
            return new TodoDto
            {
                Id = Id,
                TeamId = TeamId,
                SlackConversationId = SlackConversationId,
                ShortCode = ShortCode,
                Text = Text,
                CreatedAt = CreatedAt,
                CompletedAt = CompletedAt,
                RemovedAt = RemovedAt,
                ClaimedByUserId = ClaimedByUserId
            };
        }
    }
}
