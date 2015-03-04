using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Authentication.Forms;
using SlashTodo.Core.Domain;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Web.Lookups;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Authentication
{
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule(
            IViewModelFactory viewModelFactory, 
            ISlackSettings slackSettings, 
            IOAuthState oAuthState,
            ISlackApi slackApi,
            AccountKit accountKit,
            UserKit userKit)
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
                // Handle authentication error
                if (!string.IsNullOrEmpty((string)Request.Query["error"]))
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // Validate the state parameter (prevent CSRF)
                var state = (string)Request.Query["state"];
                if (string.IsNullOrWhiteSpace(state) ||
                    !oAuthState.Validate(state))
                {
                    // TODO Log invalid state - possible abuse
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // Request an access token from Slack
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

                // Request basic user identity info from Slack
                var authTest = slackApi.AuthTest(new AuthTestRequest
                {
                    AccessToken = oAuthAccess.AccessToken
                }).Result; // TODO Await instead
                if (!authTest.Ok)
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // Get the user object from Slack
                var usersInfo = slackApi.UsersInfo(new UsersInfoRequest
                {
                    AccessToken = oAuthAccess.AccessToken,
                    UserId = authTest.UserId
                }).Result; // TODO Await instead
                if (!usersInfo.Ok)
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // If the user is not (at least) team admin, deny access.
                if (!usersInfo.User.IsAdmin)
                {
                    // TODO Log
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // Get the account associated with the user's team, create if it
                // does not already exist.
                var account = accountKit.Query.BySlackTeamId(authTest.TeamId) ??
                              Account.Create(Guid.NewGuid(), authTest.TeamId);

                account.UpdateSlackTeamName(authTest.TeamName);
                accountKit.Repository.Save(account).Wait(); // TODO Await instead

                // Get the user, create if it does not already exist.
                var user = userKit.Query.BySlackUserId(authTest.UserId) ??
                           Core.Domain.User.Create(Guid.NewGuid(), account.Id, authTest.UserId);

                // Store the access token with the user.
                user.UpdateSlackApiAccessToken(oAuthAccess.AccessToken);
                user.UpdateSlackUserName(authTest.UserName);
                userKit.Repository.Save(user).Wait(); // TODO Await instead

                // Login and redirect to account page
                return this.LoginAndRedirect(user.Id, fallbackRedirectUrl: "/account");
            };
        }
    }
}