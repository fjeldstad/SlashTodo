using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Security;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Account.ViewModels;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Account
{
    public class AccountModule : NancyModule
    {
        public AccountModule(
            IViewModelFactory viewModelFactory,
            IHostSettings hostSettings, 
            AccountKit accountKit)
            : base("/account")
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);
            this.RequiresAuthentication();

            Get["/"] = _ =>
            {
                var currentSlackUser = (SlackUserIdentity)Context.CurrentUser;
                var account = accountKit.Repository.GetById(currentSlackUser.AccountId).Result; // TODO Await instead
                var viewModel = viewModelFactory.Create<DashboardViewModel>();
                viewModel.SlackTeamName = account.SlackTeamName;
                viewModel.SlashCommandToken = account.SlashCommandToken;
                viewModel.IncomingWebhookUrl = account.IncomingWebhookUrl.AbsoluteUri;
                return View["Dashboard.cshtml", viewModel];
            };
        }
    }
}