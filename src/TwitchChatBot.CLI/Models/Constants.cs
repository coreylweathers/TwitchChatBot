namespace TwitchChatBot.CLI.Models
{
    public class Constants
    {
        public const string CONFIG_SIGNALR_URI = "SignalR:Uri";

        public const string CONFIG_TWITCH_USERNAME = "Twitch:Username";
        public const string CONFIG_TWITCH_PASSWORD = "Twitch:Password";
        public const string CONFIG_TWITCH_CHANNELS = "Twitch:Channels";
        public const string CONFIG_TWITCH_CLIENTID = "Twitch:ClientId";
        public const string CONFIG_TWITCH_CLIENTSECRET = "Twitch:ClientSecret";
        public const string CONFIG_TWITCH_TOKENURI = "Twitch:Uris:Token";
        public const string CONFIG_TWITCH_STREAMURI = "Twitch:Uris:Stream";


        public const string CONFIG_TABLE_STREAMINGTABLE = "Table:StreamingTable";
        public const string CONFIG_TABLE_CONNECTIONSTRING = "Table:ConnectionString";
        public const string CONFIG_TABLE_METRICSTABLE = "Table:MetricsTable";

        public const string DATETIME_FORMAT = "o";

        public const string ZAPIER_EVENTTYPE_MESSAGE = "Message";
        public const string ZAPIER_EVENTTYPE_STREAMSUMMARY = "StreamSummary";
        public const string CONFIG_ZAPIER_MESSAGEURL = "Zapier:MessageUrl";
        public const string CONFIG_ZAPIER_STREAMSUMMARYURL = "Zapier:StreamSummaryUrl";
    }
}
