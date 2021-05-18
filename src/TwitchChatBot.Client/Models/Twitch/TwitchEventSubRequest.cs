using System.Text.Json.Serialization;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchEventSubRequest
    {
        public string Type { get; set; } = "stream.online";
        public string Version { get; set; } = "1";
        public RequestCondition Condition { get; set; }
        public RequestTransport Transport { get; set; }

        public class RequestCondition
        {
            [JsonPropertyName("broadcaster_user_id")]
            public string BroadcasterUserId { get; set; }
        }


        public class RequestTransport
        {
            public string Method { get; set; } = "webhook";
            public string Callback { get; set; }
            public string Secret { get; set; }
        }
    }
}
