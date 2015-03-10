using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Security;

namespace SlashTodo.Web.Security
{
    public class NancyUserIdentity : IUserIdentity
    {
        public Guid Id { get; set; }
        public string SlackUserId { get; set; }
        public string SlackTeamId { get; set; }
        public string SlackUserName { get; set; }

        public string UserName
        {
            get { return SlackUserName; }
        }

        public IEnumerable<string> Claims
        {
            get { throw new NotSupportedException(); }
        }
    }
}