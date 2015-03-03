using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Authentication
{
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule(IViewModelFactory viewModelFactory, ISlackSettings slackSettings, IOAuthState oAuthState)
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

            Get["/authenticate"] = _ =>
            {
                if (!string.IsNullOrEmpty((string)Request.Query["error"]))
                {
                    return View["AuthenticationFailed.cshtml", viewModelFactory.Create<EmptyViewModel>()];
                }
                throw new NotImplementedException();
            };
        }
    }
}