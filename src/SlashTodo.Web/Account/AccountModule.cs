using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.ModelBinding;
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

            Post["/settings", true] = async (_, ct) =>
            {
                var settings = this.Bind<SettingsViewModel>();
                if (settings == null || 
                    (settings.IncomingWebhookUrl != null && !settings.IncomingWebhookUrl.IsAbsoluteUri))
                {
                    return HttpStatusCode.BadRequest.WithReasonPhrase("Unable to extract Slash Command Token and Incoming Webhook Url from request body.");
                }
                var currentSlackUser = (SlackUserIdentity)Context.CurrentUser;
                var account = await accountKit.Repository.GetById(currentSlackUser.AccountId);
                if (account == null)
                {
                    return HttpStatusCode.NotFound.WithReasonPhrase("The account does not exist. Try singing out and back in again.");
                }
                account.UpdateSlashCommandToken(settings.SlashCommandToken);
                account.UpdateIncomingWebhookUrl(settings.IncomingWebhookUrl);
                await accountKit.Repository.Save(account);
                return HttpStatusCode.OK;
            };
        }
    }
}