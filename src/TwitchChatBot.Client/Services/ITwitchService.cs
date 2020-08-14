using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Twitch;

namespace TwitchChatBot.Client.Services
{
    public interface ITwitchService
    {
        string AccessToken { get; set; }
        List<TwitchUser> TwitchUsers { get; set; }
        Task SetAccessToken(string token);
        Task LoadChannelData(List<string> channels);
        Task GetCurrentSubscriptions();
        Task<bool> UpdateFollowerSubscription(IEnumerable<string> channels, SubscriptionStatus status);
        Task<bool> UpdateStreamChangeSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus);
    }
}
