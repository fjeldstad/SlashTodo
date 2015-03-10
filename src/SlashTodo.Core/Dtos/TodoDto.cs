using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Dtos
{
    public class TodoDto
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
    }
}
