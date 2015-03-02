using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Core.Domain
{
    public class Account : Aggregate
    {
        public Account()
        {
            
        }

        public static Account Create(Guid id, string teamId)
        {
            throw new NotImplementedException();
        }

        protected override void ApplyEventCore(IDomainEvent @event)
        {
            throw new NotImplementedException();
        }
    }
}
