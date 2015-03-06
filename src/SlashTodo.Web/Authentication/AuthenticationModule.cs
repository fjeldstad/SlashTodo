using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SlashTodo.Core.Domain;
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
            ISlackApi slackApi,
            AccountKit accountKit,
            UserKit userKit,
            IHostSettings hostSettings)
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);

            Get["/signin"] = _ =>
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

            Get["/signout"] = _ =>
            {
                return this.LogoutAndRedirect("/");
            };

            Get["/authenticate", true] = async (_,ct) =>
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
                var oAuthAccess = await slackApi.OAuthAccess(new OAuthAccessRequest
                {
                    ClientId = slackSettings.ClientId,
                    ClientSecret = slackSettings.ClientSecret,
                    Code = (string)Request.Query["code"],
                    RedirectUri = slackSettings.OAuthRedirectUrl.AbsoluteUri
                });
                if (!oAuthAccess.Ok)
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // Request basic user identity info from Slack
                var authTest = await slackApi.AuthTest(new AuthTestRequest
                {
                    AccessToken = oAuthAccess.AccessToken
                });
                if (!authTest.Ok)
                {
                    // TODO Log error
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }

                // Get the user object from Slack
                var usersInfo = await slackApi.UsersInfo(new UsersInfoRequest
                {
                    AccessToken = oAuthAccess.AccessToken,
                    UserId = authTest.UserId
                });
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
                var accountId = await accountKit.Lookup.BySlackTeamId(authTest.TeamId);
                Core.Domain.Account account = null;
                if (accountId.HasValue)
                {
                    account = await accountKit.Repository.GetById(accountId.Value);
                }
                if (account == null)
                {
                    account = Core.Domain.Account.Create(Guid.NewGuid(), authTest.TeamId);
                }
                account.UpdateSlackTeamName(authTest.TeamName);
                await accountKit.Repository.Save(account);
                              
                // Get the user, create if it does not already exist.
                var userId = await userKit.Lookup.BySlackUserId(authTest.UserId);
                Core.Domain.User user = null;
                if (userId.HasValue)
                {
                    user = await userKit.Repository.GetById(userId.Value);
                }
                if (user == null)
                {
                    user = Core.Domain.User.Create(Guid.NewGuid(), account.Id, authTest.UserId);
                }

                // Store the access token with the user.
                user.UpdateSlackApiAccessToken(oAuthAccess.AccessToken);
                user.UpdateSlackUserName(authTest.UserName);
                await userKit.Repository.Save(user);

                // Login and redirect to account page
                return this.LoginAndRedirect(user.Id, fallbackRedirectUrl: "/account");
            };
        }
    }
}