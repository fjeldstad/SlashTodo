using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Security;

namespace SlashTodo.Web
{
    public class SlackUserIdentity : IUserIdentity
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string UserName { get { return SlackUserName; } }
        public string SlackUserName { get; set; }
        public string SlackTeamName { get; set; }
        public string SlackUserId { get; set; }
        public string SlackTeamId { get; set; }
        public string SlackApiAccessToken { get; set; }

        public IEnumerable<string> Claims
        {
            get { throw new NotSupportedException(); }
        }
    }
}