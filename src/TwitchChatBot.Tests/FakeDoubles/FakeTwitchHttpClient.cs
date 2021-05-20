using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Client.Models.Twitch.Enums;
using TwitchChatBot.Client.Services;

namespace TwitchChatBot.Tests.FakeDoubles
{
    public class FakeTwitchHttpClient : ITwitchHttpClient
    {
        public Task<HttpStatusCode> UpdateSubscription(TwitchWebhookRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<TwitchUser>> GetTwitchChannels(List<string> channelLogins)
        {
            return Task.FromResult(new List<TwitchUser>
            {
                new TwitchUser
                {
                    BroadcasterType = "blah",
                    Description = "blah blah"
                }
            });
        }

        public Task<Dictionary<string, TwitchSubscriptionStatus>> GetSubscriptionData(IEnumerable<string> channelIds)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> SubscribeToChannelEvents(List<string> channelLogins)
        {
            throw new System.NotImplementedException();
        }

        public Task<TwitchBanResponse> GetBannedUsers(string broadcasterId)
        {
            throw new System.NotImplementedException();
        }
    }
}