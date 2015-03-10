using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.Security
{
    public interface INancyUserIdentityIdProvider
    {
        Guid GenerateNewId();
    }

    public class DefaultNancyUserIdentityIdProvider : INancyUserIdentityIdProvider
    {
        public Guid GenerateNewId()
        {
            return Guid.NewGuid();
        }
    }
}
