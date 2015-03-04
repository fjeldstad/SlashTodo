using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Authentication.Forms;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Authentication
{
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule(
            IViewModelFactory viewModelFactory, 
            ISlackSettings slackSettings, 
            IOAuthState oAuthState,
            ISlackApi slackApi)
        {
            Get["/login"] = _ =>
            {
                var authorizationUrl = new UriBuilder(slackSettings.OAuthAuthorizationUrl);
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["client_id"] = slackSettings.ClientId;
                query["state"] = oAuthState.Generate();
                query["scope"] = slackSettings.OAuthScope;
                query["redirect_uri"] = slackSettings.OAuthRedirectUrl.AbsoluteUri;
                authorizationUrl.Query = query.ToString();
                return Response.AsRedirect(authorizationUrl.Uri.AbsoluteUri);
            };

            Get["/logout"] = _ =>
            {
                return this.LogoutAndRedirect("/");
            };

            Get["/authenticate"] = _ =>
            {
                if (!string.IsNullOrEmpty((string)Request.Query["error"]))
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }
                var state = (string)Request.Query["state"];
                if (string.IsNullOrWhiteSpace(state) ||
                    !oAuthState.Validate(state))
                {
                    // TODO Log invalid state - possible abuse
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }
                var oAuthAccess = slackApi.OAuthAccess(new OAuthAccessRequest
                {
                    ClientId = slackSettings.ClientId,
                    ClientSecret = slackSettings.ClientSecret,
                    Code = (string)Request.Query["code"],
                    RedirectUri = slackSettings.OAuthRedirectUrl.AbsoluteUri
                }).Result; // TODO Await instead
                if (!oAuthAccess.Ok)
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }
                var teamAndUserInfo = slackApi.AuthTest(new AuthTestRequest
                {
                    AccessToken = oAuthAccess.AccessToken
                }).Result; // TODO Await instead
                // TODO Get the account associated with the user's team (by SlackTeamId).
                // TODO If the account does not exist, create it.
                // TODO Get the user (by SlackUserId).
                // TODO If the user does not exist, create it.
                // TODO Store the access token with the user.
                // TODO Login and redirect to account page
                throw new NotImplementedException();
            };
        }
    }
}