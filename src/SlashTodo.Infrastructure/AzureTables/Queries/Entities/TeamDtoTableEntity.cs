using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Infrastructure.AzureTables.Queries.Entities
{
    public class TeamDtoTableEntity : TableEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SlackUrl { get; set; }
        public string SlashCommandToken { get; set; }
        public string IncomingWebhookUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool IsActive { get { return ActivatedAt.HasValue; } }

        public TeamDtoTableEntity()
        {
        }

        public TeamDtoTableEntity(TeamDto dto)
            : this(dto, x => x.Id, x => x.Id)
        {
        }

        public TeamDtoTableEntity(TeamDto dto, Func<TeamDto, string> partitionKey, Func<TeamDto, string> rowKey)
        {
            PartitionKey = partitionKey(dto);
            RowKey = rowKey(dto);

            Id = dto.Id;
            Name = dto.Name;
            SlackUrl = dto.SlackUrl != null ? dto.SlackUrl.AbsoluteUri : null;
            SlashCommandToken = dto.SlashCommandToken;
            IncomingWebhookUrl = dto.IncomingWebhookUrl != null ? dto.IncomingWebhookUrl.AbsoluteUri : null;
            CreatedAt = dto.CreatedAt;
            ActivatedAt = dto.ActivatedAt;
        }

        public TeamDto GetDto()
        {
            return new TeamDto
            {
                Id = Id,
                Name = Name,
                SlackUrl = !string.IsNullOrEmpty(SlackUrl) ? new Uri(SlackUrl) : null,
                SlashCommandToken = SlashCommandToken,
                IncomingWebhookUrl = !string.IsNullOrEmpty(IncomingWebhookUrl) ? new Uri(IncomingWebhookUrl) : null,
                CreatedAt = CreatedAt,
                ActivatedAt = ActivatedAt
            };
        }
    }
}
