using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlashTodo.Infrastructure.Slack
{
    public interface ISlackIncomingWebhookApi
    {
        Task Send(Uri incomingWebhookUrl, SlackIncomingWebhookMessage message);
    }

    public class SlackIncomingWebhookMessage
    {
        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty(PropertyName = "icon_emoji")]
        public string IconEmoji { get; set; }

        [JsonProperty(PropertyName = "channel")]
        public string ConversationId { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "unfurl_links")]
        public bool UnfurlLinks { get; set; }

        [JsonProperty(PropertyName = "attachments")]
        public SlackMessageAttachment[] Attachments { get; set; }

        [JsonProperty(PropertyName = "image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty(PropertyName = "mrkdwn")]
        public bool EnableMarkdown { get; set; }

        public SlackIncomingWebhookMessage()
        {
            Attachments = new SlackMessageAttachment[0];
            EnableMarkdown = true;
        }

        [Serializable]
        public class SlackMessageAttachment
        {
            [JsonProperty(PropertyName = "fallback")]
            public string Fallback { get; set; }

            [JsonProperty(PropertyName = "pretext")]
            public string PreText { get; set; }

            [JsonProperty(PropertyName = "text")]
            public string Text { get; set; }

            [JsonProperty(PropertyName = "color")]
            public string Color { get; set; }

            [JsonProperty(PropertyName = "mrkdwn_in")]
            public string[] EnableMarkdownInProperties { get; set; }

            [JsonProperty(PropertyName = "fields")]
            public SlackMessageAttachmentField[] Fields { get; set; }

            public SlackMessageAttachment()
            {
                EnableMarkdownInProperties = new string[0];
                Fields = new SlackMessageAttachmentField[0];
            }

            [Serializable]
            public class SlackMessageAttachmentField
            {
                [JsonProperty(PropertyName = "title")]
                public string Title { get; set; }

                [JsonProperty(PropertyName = "value")]
                public string Value { get; set; }

                [JsonProperty(PropertyName = "short")]
                public bool Short { get; set; }
            }
        }
    }
}
