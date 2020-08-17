using System.ComponentModel.DataAnnotations;

namespace TwitchChatBot.Client.Models.Component
{
    public class TwitchChannelModel
    {
        public string ChannelName { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFollowerSubscribed { get; set; }
        public bool IsStreamSubscribed { get; set; }
    }
}
