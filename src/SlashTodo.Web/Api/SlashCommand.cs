using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SlashTodo.Web.Api
{
    public class SlashCommand
    {
        public string Token { get; set; }
        public string TeamId { get; set; }
        public string TeamDomain { get; set; }
        public string ConversationId { get; set; }
        public string ConversationName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Command { get; set; }
        public string Text { get; set; }

        public class Raw
        {
            public string token { get; set; }
            public string team_id { get; set; }
            public string team_domain { get; set; }
            public string channel_id { get; set; }
            public string channel_name { get; set; }
            public string user_id { get; set; }
            public string user_name { get; set; }
            public string command { get; set; }
            public string text { get; set; }
        }
    }

    public static class RawSlashCommandExtensions
    {
        public static SlashCommand ToSlashCommand(this SlashCommand.Raw raw)
        {
            return raw == null ?
                null :
                new SlashCommand
                {
                    Token = raw.token,
                    TeamId = raw.team_id,
                    TeamDomain = raw.team_domain,
                    ConversationId = raw.channel_id,
                    ConversationName = raw.channel_name,
                    UserId = raw.user_id,
                    UserName = raw.user_name,
                    Command = raw.command,
                    Text = raw.text
                };
        }
    }
}