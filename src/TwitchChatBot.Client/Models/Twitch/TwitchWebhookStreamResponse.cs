using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchWebhookStreamResponse
    {
        [JsonProperty("game_id")]
        public string GameId { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("started_at")]
        public DateTime StartedAt { get; set; }
        [JsonProperty("tag_ids")]
        public List<Guid> TagIds { get; set; }
        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        [JsonProperty("user_name")]
        public string UserName { get; set; }
        [JsonProperty("viewer_count")]
        public int ViewerCount { get; set; }
    }
}
