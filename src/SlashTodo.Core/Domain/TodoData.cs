using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public class TodoData
    {
        public string Text { get; set; }
        public string State { get; set; }
        public string ClaimedBy { get; set; }
    }
}
