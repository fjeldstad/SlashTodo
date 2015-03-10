using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public class TodoEventVersionMismatchException : Exception
    {
    }

    public class TodoClaimedBySomeoneElseException : Exception
    {
        public string ClaimedByUserId { get; private set; }

        public TodoClaimedBySomeoneElseException(string claimedByUserId)
        {
            ClaimedByUserId = claimedByUserId;
        }
    }
}
