using System.Timers;

namespace TwitchChatBot.CLI.Models
{
    public class TwitchChannel
    {
        public string Channel { get; set; }
        public Timer Timer { get; set; }
    }
}
