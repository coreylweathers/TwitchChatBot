using System.Collections.Generic;

namespace TwitchChatBot.Client.Models.Options
{
    public class TwitchOptions
    {
        public string CallbackTemplate { get; set; }
        public List<string> Channels { get; set; }
        public string WebhookSecret { get; set; }
        public Urls Urls { get; set; }

    }

    public class Urls
    {
        public string ApiUrl { get; set; }
        public string EventSubscriptionUrl { get; set; }
        public string UserUrl { get; set; }
        public string WebhookSubscriptionUrl { get; set; }
    }
}
