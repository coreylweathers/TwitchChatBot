using Microsoft.Azure.Cosmos.Table;

namespace TwitchChatBot.Shared.Models.Entities
{
    public class SubscriptionActivityEntity : TableEntity
    {
        // PARTITION KEY = channel
        // ROWKEY = DateTime.UtcNow
        public string Activity { get; set; }
        public string State { get; set; }
    }
}
