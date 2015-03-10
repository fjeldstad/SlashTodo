using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.Security
{
    public interface INancyUserIdentityService
    {
        Task<NancyUserIdentity> GetOrCreate(string slackUserId, string slackTeamId, string slackUserName);
    }
}
