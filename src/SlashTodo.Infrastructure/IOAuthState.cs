using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure
{
    public interface IOAuthState
    {
        string Generate();
        bool Validate(string state);
    }
}
