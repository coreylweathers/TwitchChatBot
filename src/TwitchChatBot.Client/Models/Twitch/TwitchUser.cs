using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchUser
    {
        public string Id { get; set; }
        [JsonProperty("login")]
        [JsonPropertyName("login")]
        public string LoginName { get; set; }
        [JsonProperty("display_name")]
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
        public string Type { get; set; }
        [JsonProperty("broadcaster_type")]
        [JsonPropertyName("broadcaster_type")]
        public string BroadcasterType { get; set; }
        public string Description { get; set; }
        [JsonProperty("profile_image_url")]
        [JsonPropertyName("profile_image_url")]
        public string ProfileImageUrl { get; set; }
        [JsonProperty("offline_image_url")]
        [JsonPropertyName("offline_image_url")]
        public string OfflineImageUrl { get; set; }
        [JsonProperty("view_count")]
        [JsonPropertyName("view_count")]
        public int ViewCount { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsStreamSubscribed { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsFollowSubscribed { get; set; }
    }
}
