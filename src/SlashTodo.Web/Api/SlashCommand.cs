using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SlashTodo.Web.Api
{
    public class SlashCommand
    {
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        [JsonProperty(PropertyName = "team_id")]
        public string TeamId { get; set; }

        [JsonProperty(PropertyName = "team_domain")]
        public string TeamDomain { get; set; }

        [JsonProperty(PropertyName = "channel_id")]
        public string ConversationId { get; set; }

        [JsonProperty(PropertyName = "channel_name")]
        public string ConversationName { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "user_name")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "command")]
        public string Command { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }
}