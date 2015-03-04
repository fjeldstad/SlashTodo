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
        public Guid ClaimedByUserId { get; private set; }

        public TodoClaimedBySomeoneElseException(Guid claimedByUserId)
        {
            ClaimedByUserId = claimedByUserId;
        }
    }
}
