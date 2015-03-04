using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Security;
using SlashTodo.Infrastructure.Configuration;

namespace SlashTodo.Web.Account
{
    public class AccountModule : NancyModule
    {
        public AccountModule(IHostSettings hostSettings, AccountKit accountKit)
            : base("/account")
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);
            this.RequiresAuthentication();

            Get["/"] = _ =>
            {
                throw new NotImplementedException();
            };
        }
    }
}