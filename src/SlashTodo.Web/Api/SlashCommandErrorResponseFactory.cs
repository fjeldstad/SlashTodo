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

        public SlashCommandErrorResponseFactory(IHostSettings hostSettings)
        {
            _hostSettings = hostSettings;
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
    }
}