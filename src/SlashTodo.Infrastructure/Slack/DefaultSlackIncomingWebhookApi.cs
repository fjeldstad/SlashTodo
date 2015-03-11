using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlashTodo.Infrastructure.Slack
{
    public class DefaultSlackIncomingWebhookApi : ISlackIncomingWebhookApi
    {
        public async Task Send(Uri incomingWebhookUrl, SlackIncomingWebhookMessage message)
        {
            using (var httpClient = new HttpClient())
            {
                var jsonContent = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(incomingWebhookUrl, jsonContent).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
