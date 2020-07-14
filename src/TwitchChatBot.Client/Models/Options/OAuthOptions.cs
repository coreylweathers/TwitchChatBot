using System.Collections.Generic;

namespace TwitchChatBot.Client.Models.Options
{
    public class OAuthOptions
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public List<string> Scopes { get; set; }
        public string ResponseType { get; set; }
    }
}
