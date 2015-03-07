using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Infrastructure.Messaging
{
    public interface ISubscriber : IDisposable
    {
        void RegisterSubscriptions(ISubscriptionRegistry registry);
    }
}
