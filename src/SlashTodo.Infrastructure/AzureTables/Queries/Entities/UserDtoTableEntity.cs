using System;
using Microsoft.WindowsAzure.Storage.Table;
using SlashTodo.Core.Dtos;

namespace SlashTodo.Infrastructure.AzureTables.Queries.Entities
{
    public class UserDtoTableEntity : TableEntity
    {
        public string Id { get; set; }
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string SlackApiAccessToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }

        public UserDtoTableEntity()
        {
        }

        public UserDtoTableEntity(UserDto dto)
            : this(dto, x => x.Id, x => x.Id)
        {
        }

        public UserDtoTableEntity(UserDto dto, Func<UserDto, string> partitionKey, Func<UserDto, string> rowKey)
        {
            PartitionKey = partitionKey(dto);
            RowKey = rowKey(dto);

            Id = dto.Id;
            TeamId = dto.TeamId;
            Name = dto.Name;
            SlackApiAccessToken = dto.SlackApiAccessToken;
            CreatedAt = dto.CreatedAt;
            ActivatedAt = dto.ActivatedAt;
        }

        public UserDto GetDto()
        {
            return new UserDto
            {
                Id = Id,
                TeamId = TeamId,
                Name = Name,
                SlackApiAccessToken = SlackApiAccessToken,
                CreatedAt = CreatedAt,
                ActivatedAt = ActivatedAt
            };
        }
    }
}
