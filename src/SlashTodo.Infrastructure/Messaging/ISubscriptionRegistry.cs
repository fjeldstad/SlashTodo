using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure.Messaging
{
    public interface ISubscriptionRegistry
    {
        ISubscriptionToken RegisterSubscription<TMessage>(Action<TMessage> action);
    }
}
