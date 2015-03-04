using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.Lookups
{
    public interface IAccountLookup
    {
        Guid? BySlackTeamId(string slackTeamId);
    }
}
