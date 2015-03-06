using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Core.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string SlackUserId { get; set; }
        public string SlackUserName { get; set; }
        public string SlackApiAccessToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool IsActive { get { return ActivatedAt.HasValue; } }
    }
}