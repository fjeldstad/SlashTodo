using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Core.Dtos
{
    public class UserDto
    {
        public string Id { get; set; }
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string SlackApiAccessToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool IsActive { get { return ActivatedAt.HasValue; } }
    }
}