using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SlashTodo.Web.ViewModels;

namespace SlashTodo.Web.Account.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        public string SlashCommandUrl { get; set; }
        public string SlackTeamName { get; set; }
        public string SlackTeamUrl { get; set; }
        public string IncomingWebhookUrl { get; set; }
        public string SlashCommandToken { get; set; }
    }
}