using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core
{
    public class TodoContext
    {
        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
    }
}
