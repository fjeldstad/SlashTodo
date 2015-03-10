using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using SlashTodo.Core;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.Account.ViewModels;
using SlashTodo.Web.Security;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Account
{
    public class AccountModule : NancyModule
    {
        public AccountModule(
            IViewModelFactory viewModelFactory,
            IAppSettings appSettings,
            IHostSettings hostSettings,
            QueryTeams.IById queryTeamsById,
            IRepository<Core.Domain.Team> teamRepository)
            : base("/account")
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);
            this.RequiresAuthentication();

            Get["/", true] = async (_,ct) =>
            {
                var currentSlackUser = (NancyUserIdentity)Context.CurrentUser;
                var team = await queryTeamsById.ById(currentSlackUser.SlackTeamId);
                var viewModel = viewModelFactory.Create<DashboardViewModel>();
                viewModel.SlackTeamName = team.Name;
                if (team.SlackUrl != null)
                {
                    viewModel.SlackTeamUrl = team.SlackUrl.AbsoluteUri;
                }
                viewModel.SlashCommandToken = team.SlashCommandToken;
                if (team.IncomingWebhookUrl != null)
                {
                    viewModel.IncomingWebhookUrl = team.IncomingWebhookUrl.AbsoluteUri;
                }
                viewModel.SlashCommandUrl = string.Format("{0}/api/{1}", hostSettings.BaseUrl.TrimEnd('/'), team.Id);
                viewModel.HelpEmailAddress = appSettings.Get("misc:HelpEmailAddress");

                return Negotiate
                    .WithModel(viewModel)
                    .WithView("Dashboard.cshtml");
            };

            Post["/slash-command-token", true] = async (_, ct) =>
            {
                var viewModel = this.Bind<UpdateSlashCommandTokenViewModel>();
                var currentSlackUser = (NancyUserIdentity)Context.CurrentUser;
                var team = await teamRepository.GetById(currentSlackUser.SlackTeamId);
                if (team == null)
                {
                    return HttpStatusCode.NotFound.WithReasonPhrase("The account does not exist. Try singing out and back in again.");
                }
                team.UpdateSlashCommandToken(viewModel.SlashCommandToken);
                await teamRepository.Save(team);
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
                var currentSlackUser = (NancyUserIdentity)Context.CurrentUser;
                var team = await teamRepository.GetById(currentSlackUser.SlackTeamId);
                if (team == null)
                {
                    return HttpStatusCode.NotFound.WithReasonPhrase("The account does not exist. Try singing out and back in again.");
                }
                team.UpdateIncomingWebhookUrl(incomingWebhookUrl);
                await teamRepository.Save(team);
                return HttpStatusCode.OK;
            };
        }
    }
}