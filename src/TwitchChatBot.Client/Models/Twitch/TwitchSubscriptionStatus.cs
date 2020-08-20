using System;

namespace TwitchChatBot.Client.Models.Twitch.Enums
{
    [Flags]
    public enum TwitchSubscriptionStatus
    {
        None,
        FollowerSubscription,
        StreamSubscription
    }
}