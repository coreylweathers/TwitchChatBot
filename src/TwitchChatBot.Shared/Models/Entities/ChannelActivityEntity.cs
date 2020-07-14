using Microsoft.Azure.Cosmos.Table;
using System.Text.Json;

namespace TwitchChatBot.Shared.Models.Entities
{
    public class ChannelActivityEntity : TableEntity
    {
        public string Viewer { get; set; }
        public string Activity { get; set; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
