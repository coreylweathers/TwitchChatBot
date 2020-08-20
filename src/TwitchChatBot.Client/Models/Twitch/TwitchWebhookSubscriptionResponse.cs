using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchWebhookSubscriptionResponse
    {
        public Uri Topic { get; set; }
        public Uri Callback { get; set; }
        [JsonProperty("expires_at")]
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
