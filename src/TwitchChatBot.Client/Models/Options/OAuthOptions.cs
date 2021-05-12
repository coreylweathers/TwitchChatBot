using System.Collections.Generic;

namespace TwitchChatBot.Client.Models.Options
{
    public class OAuthOptions
    {
        public string TokenUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public List<string> Scopes { get; set; }
    }
}
