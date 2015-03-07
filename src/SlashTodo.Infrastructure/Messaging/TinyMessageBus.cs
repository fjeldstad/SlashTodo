using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Messaging
{
    public class TinyMessageBus : IMessageBus, ISubscriptionRegistry, IEventDispatcher
    {
        private readonly ITinyMessengerHub _hub;

        public TinyMessageBus(ITinyMessengerHub hub)
        {
            _hub = hub;
        }

        public Task Publish<TMessage>(TMessage message)
        {
            var tcs = new TaskCompletionSource<object>();
            _hub.PublishAsync(new Message<TMessage>(message), ar => tcs.SetResult(null));
            return tcs.Task;
        }

        public ISubscriptionToken Subscribe<TMessage>(Action<TMessage> action)
        {
            return new SubscriptionToken(_hub.Subscribe<Message<TMessage>>(message => action(message.Content)));
        }

        Task IEventDispatcher.Publish(Core.Domain.IDomainEvent @event)
        {
            return Publish((dynamic)@event);
        }

        ISubscriptionToken ISubscriptionRegistry.RegisterSubscription<TMessage>(Action<TMessage> action)
        {
            return Subscribe(action);
        }

        public class Message<TContent> : ITinyMessage
        {
            private readonly TContent _content;

            public TContent Content { get { return _content; } }
            public object Sender { get { return null; } }

            public Message(TContent content)
            {
                _content = content;
            }
        }

        public class SubscriptionToken : ISubscriptionToken
        {
            private readonly TinyMessageSubscriptionToken _token;

            public SubscriptionToken(TinyMessageSubscriptionToken token)
            {
                _token = token;
            }

            public void Dispose()
            {
                _token.Dispose();
            }
        }
    }
}
