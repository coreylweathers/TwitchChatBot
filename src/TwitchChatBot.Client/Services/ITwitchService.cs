using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Twitch;

namespace TwitchChatBot.Client.Services
{
    public interface ITwitchService
    {
        string UserAccessToken { get; set; }
        string AppAccessToken { get; set; }
        List<TwitchUser> TwitchUsers { get; set; }
        Task SetUserAccessToken(string token);
        Task SetAppAccessToken(string clientId, string clientSecret);
        Task LoadChannelData(string channel = null);
        Task GetCurrentSubscriptions(IEnumerable<string> channelIds);
        Task<bool> UpdateFollowerSubscription(IEnumerable<string> channels, SubscriptionStatus status);
        Task<bool> UpdateStreamChangeSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus);
    }
}
