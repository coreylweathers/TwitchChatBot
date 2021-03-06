﻿using System.ComponentModel.DataAnnotations;
using System;

namespace TwitchChatBot.Client.Models.Component
{
    public class TwitchChannelModel
    {
        public string ChannelName { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFollowerSubscribed { get; set; }
        public bool IsStreamSubscribed { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is TwitchChannelModel instance))
            {
                return false;
            }
            return (ChannelName.Equals(instance.ChannelName, StringComparison.InvariantCultureIgnoreCase)
                && ImageUrl.Equals(instance.ImageUrl, StringComparison.InvariantCultureIgnoreCase)
                && IsFollowerSubscribed == instance.IsFollowerSubscribed
                && IsStreamSubscribed == instance.IsStreamSubscribed);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
