using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwitchChatBot.Client.Models.Twitch
{
    public class TwitchEventSubResponse
    {
        public ResponseSubscription Subscription {get;set;}
        public string Challenge {get;set;}

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        public class ResponseSubscription 
        {
            public string Id {get;set;}
            public string Status {get;set;}
            public string Type {get;set;}
            public string Version {get;set;}
            public ResponseCondition Condition {get;set;}
            public ResponseTransport Transport {get;set;}
            [JsonPropertyName("created_at")]
            public DateTime CreatedAt {get;set;}
            public int Cost {get;set;}
        }
        public class ResponseTransport 
        {
            public string Method {get;set;}
            public string Callback {get;set;}
        }
        public class ResponseCondition
        {
            [JsonPropertyName("broadcaster_user_id")]
            public string BroadcasterUserId {get;set;}
        }
    }
}