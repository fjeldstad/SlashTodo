using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core
{
    public interface IMessageBus
    {
        Task Publish(IMessage message);
        ISubscriptionToken Subscribe<TMessage>(Action<TMessage> action) where TMessage : IMessage;
    }

    public interface IMessage
    {
    }

    public interface ISubscriptionToken
    {
        void Unsubscribe();
    }
}
