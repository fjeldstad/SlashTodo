using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Account.ViewModels;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Account
{
    public class AccountModule : NancyModule
    {
        public AccountModule(
            IViewModelFactory viewModelFactory,
            IAppSettings appSettings,
            IHostSettings hostSettings,
            QueryTeams.IById queryTeamsById)
            : base("/account")
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);
            this.RequiresAuthentication();

            Get["/", true] = async (_,ct) =>
            {
                var currentSlackUser = (SlackUserIdentity)Context.CurrentUser;
                var account = await queryTeamsById.ById(currentSlackUser.AccountId);
                var viewModel = viewModelFactory.Create<DashboardViewModel>();
                viewModel.SlackTeamName = account.Name;
                if (account.SlackUrl != null)
                {
                    viewModel.SlackTeamUrl = account.SlackUrl.AbsoluteUri;
                }
                viewModel.SlashCommandToken = account.SlashCommandToken;
                if (account.IncomingWebhookUrl != null)
                {
                    viewModel.IncomingWebhookUrl = account.IncomingWebhookUrl.AbsoluteUri;
                }
                viewModel.SlashCommandUrl = string.Format("{0}/api/{1:N}", hostSettings.BaseUrl.TrimEnd('/'), account.Id);
                viewModel.HelpEmailAddress = appSettings.Get("misc:HelpEmailAddress");

                return Negotiate
                    .WithModel(viewModel)
                    .WithView("Dashboard.cshtml");
            };

            Post["/slash-command-token", true] = async (_, ct) =>
            {
                var viewModel = this.Bind<UpdateSlashCommandTokenViewModel>();
                var currentSlackUser = (SlackUserIdentity)Context.CurrentUser;
                var account = await accountKit.Repository.GetById(currentSlackUser.AccountId);
                if (account == null)
                {
                    return HttpStatusCode.NotFound.WithReasonPhrase("The account does not exist. Try singing out and back in again.");
                }
                account.UpdateSlashCommandToken(viewModel.SlashCommandToken);
                await accountKit.Repository.Save(account);
                return HttpStatusCode.OK;
            };

            Post["/incoming-webhook-url", true] = async (_, ct) =>
            {
                var viewModel = this.Bind<UpdateIncomingWebhookUrlViewModel>();
                Uri incomingWebhookUrl = null;
                if (!string.IsNullOrWhiteSpace(viewModel.IncomingWebhookUrl) &&
                    !Uri.TryCreate(viewModel.IncomingWebhookUrl, UriKind.Absolute, out incomingWebhookUrl))
                {
                    return HttpStatusCode.BadRequest.WithReasonPhrase("Invalid Incoming Webhook Url.");
                }
                var currentSlackUser = (SlackUserIdentity)Context.CurrentUser;
                var account = await accountKit.Repository.GetById(currentSlackUser.AccountId);
                if (account == null)
                {
                    return HttpStatusCode.NotFound.WithReasonPhrase("The account does not exist. Try singing out and back in again.");
                }
                account.UpdateIncomingWebhookUrl(incomingWebhookUrl);
                await accountKit.Repository.Save(account);
                return HttpStatusCode.OK;
            };
        }
    }
}