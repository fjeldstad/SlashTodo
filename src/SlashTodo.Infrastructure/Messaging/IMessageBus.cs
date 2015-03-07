using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure.Messaging
{
    public interface IMessageBus
    {
        Task Publish<TMessage>(TMessage message);
        ISubscriptionToken Subscribe<TMessage>(Action<TMessage> action);
    }
}
