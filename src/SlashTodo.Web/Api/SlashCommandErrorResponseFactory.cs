using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using SlashTodo.Infrastructure.Configuration;

namespace SlashTodo.Web.Api
{
    public class SlashCommandErrorResponseFactory : ISlashCommandErrorResponseFactory
    {
        private readonly IHostSettings _hostSettings;
        private readonly IAppSettings _appSettings;

        public SlashCommandErrorResponseFactory(IHostSettings hostSettings, IAppSettings appSettings)
        {
            _hostSettings = hostSettings;
            _appSettings = appSettings;
        }

        public Response ActiveAccountNotFound()
        {
            return new Nancy.Responses.TextResponse(
                HttpStatusCode.OK,
                string.Format("Your team does not have an active account with SlashTodo. Please ask a team administrator to setup an account at {0}", _hostSettings.BaseUrl));
        }

        public Response InvalidAccountIntegrationSettings()
        {
            return new Nancy.Responses.TextResponse(
                HttpStatusCode.OK,
                string.Format("The integration settings for `/todo` seems to be incorrect. Please ask a team administrator to double-check the configuration at {0}", _hostSettings.BaseUrl));
        }

        public Response InvalidSlashCommandToken()
        {
            return InvalidAccountIntegrationSettings();
        }

        public Response ErrorProcessingCommand()
        {
            return new Nancy.Responses.TextResponse(
                HttpStatusCode.OK,
                string.Format("Oops! Something went wrong. This could be a temporary hickup but if the problem persists, please send and e-mail to {0}.", _appSettings.Get("misc:HelpEmailAddress")));
        }
    }
}