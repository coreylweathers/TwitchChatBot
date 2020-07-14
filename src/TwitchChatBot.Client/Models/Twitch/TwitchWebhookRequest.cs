using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchWebhookRequest
    {
        [JsonProperty("hub.callback")]
        [JsonPropertyName("hub.callback")]
        public string Callback { get; set; }
        [JsonProperty("hub.mode")]
        [JsonPropertyName("hub.mode")]
        public string Mode { get; set; }
        [JsonProperty("hub.topic")]
        [JsonPropertyName("hub.topic")]
        public string Topic { get; set; }
        [JsonProperty("hub.lease_seconds")]
        [JsonPropertyName("hub.lease_seconds")]
        public int Lease { get; set; }

        public override string ToString()
        {
            // Todo: Add serializer for System.Text.Json here
            return JsonConvert.SerializeObject(this);
        }
    }
}
