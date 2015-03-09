using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Web.Account.ViewModels
{
    public class SettingsViewModel
    {
        public Uri IncomingWebhookUrl { get; set; }
        public string SlashCommandToken { get; set; }
    }
}