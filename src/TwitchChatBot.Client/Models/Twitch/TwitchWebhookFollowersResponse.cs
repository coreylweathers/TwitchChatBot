using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchWebhookFollowersResponse
    {
        [JsonProperty(PropertyName = "followed_at")]
        [JsonPropertyName("followed_at")]
        public DateTime FollowedAt { get; set; }
        [JsonProperty(PropertyName = "from_id")]
        [JsonPropertyName("from_id")]
        public string FromId { get; set; }
        [JsonProperty(PropertyName = "from_name")]
        [JsonPropertyName("from_name")]
        public string FromName { get; set; }
        [JsonProperty(PropertyName = "to_id")]
        [JsonPropertyName("to_id")]
        public string ToId { get; set; }
        [JsonProperty(PropertyName = "to_name")]
        [JsonPropertyName("to_name")]
        public string ToName { get; set; }
    }
}
