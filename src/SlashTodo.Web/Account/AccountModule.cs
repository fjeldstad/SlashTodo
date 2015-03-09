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

            Get["/", true] = async (_,ct) =>
            {
                var currentSlackUser = (SlackUserIdentity)Context.CurrentUser;
                var account = await accountKit.Query.ById(currentSlackUser.AccountId);
                var viewModel = viewModelFactory.Create<DashboardViewModel>();
                viewModel.SlackTeamName = account.SlackTeamName;
                if (account.SlackTeamUrl != null)
                {
                    viewModel.SlackTeamUrl = account.SlackTeamUrl.AbsoluteUri;
                }
                viewModel.SlashCommandToken = account.SlashCommandToken;
                if (account.IncomingWebhookUrl != null)
                {
                    viewModel.IncomingWebhookUrl = account.IncomingWebhookUrl.AbsoluteUri;
                }
                viewModel.SlashCommandUrl = string.Format("{0}/{1:N}", hostSettings.ApiBaseUrl.TrimEnd('/'), account.Id);

                return Negotiate
                    .WithModel(viewModel)
                    .WithView("Dashboard.cshtml");
            };
        }
    }
}