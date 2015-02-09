using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core
{
    public class TodoItem
    {
        public string Id { get; set; }
        public string ListId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TickedAt { get; set; }
        public string ClaimedBy { get; set; }

        public bool Done { get { return TickedAt.HasValue; } }
    }
}
