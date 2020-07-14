namespace TwitchChatBot.Client.Models.Options
{
    public class TableStorageOptions
    {
        public string ConnectionString { get; set; }
        public string StreamingTable { get; set; }
        public string SubscriptionTable { get; set; }
    }
}
