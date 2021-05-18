using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Twitch;

namespace TwitchChatBot.Client.Services
{
    public interface ITwitchService
    {
        // TODO: Delete the gray code because it's not being used
        string UserAccessToken { get; set; }
        string AppAccessToken { get; set; }
        Task<List<string>> GetBannedList(List<string> channels);
        List<TwitchUser> TwitchUsers { get; set; }
        List<string> MonitoredChannels { get; set; }

        Task LoadChannelData(string channel = null);
        Task GetCurrentSubscriptions(IEnumerable<string> channelIds);
        Task<bool> UpdateFollowerSubscription(IEnumerable<string> channels, SubscriptionStatus status);
        Task<bool> UpdateStreamChangeSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus);

        Task LoadMonitoredChannels();
        Task SubscribeToChannelEvents();
    }
}
