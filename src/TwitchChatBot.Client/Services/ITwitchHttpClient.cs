using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Client.Models.Twitch.Enums;

namespace TwitchChatBot.Client.Services
{
    public interface ITwitchHttpClient
    {
        Task<HttpStatusCode> UpdateSubscription(TwitchWebhookRequest request);
        Task<List<TwitchUser>> GetTwitchChannels(List<string> channelLogins);
        Task<Dictionary<string, TwitchSubscriptionStatus>> GetSubscriptionData(IEnumerable<string> channelIds);
        Task<bool> SubscribeToChannelEvents(List<string> channelLogins);
        Task<TwitchBanResponse> GetBannedUsers(string broadcasterId);
    }
}