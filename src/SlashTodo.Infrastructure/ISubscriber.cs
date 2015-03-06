using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Infrastructure
{
    public interface ISubscriber<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleEvent(TEvent @event);
    }

    public interface ISubscriber
    {
        void RegisterSubscriptions(Action<IMessageBus> bus);
    }
}
