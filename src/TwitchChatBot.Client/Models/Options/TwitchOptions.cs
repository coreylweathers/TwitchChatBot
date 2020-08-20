using System.Collections.Generic;

namespace TwitchChatBot.Client.Models.Options
{
    public class TwitchOptions
    {
        public string FollowerCallbackTemplate { get; set; }
        public string FollowerTopicTemplate { get; set; }
        public int DefaultLease { get; set; }
        public string StreamChangeCallbackTemplate { get; set; }
        public string StreamChangeTopicTemplate { get; set; }
        public string WebhookSubscriptionUrl { get; set; }
        public string WebhookSubscriptionsApiUrl { get; set; }
        public string UserUrl { get; set; }
        public List<string> Channels { get; set; }
    }
}
