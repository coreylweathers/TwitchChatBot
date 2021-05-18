using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Client.Models.Twitch.Enums;

namespace TwitchChatBot.Client.Services
{
    public class TwitchService : ITwitchService
    {
        

        public List<TwitchUser> TwitchUsers { get; set; }
        public string UserAccessToken { get; set; }
        public string AppAccessToken { get; set; }
        public List<string> MonitoredChannels { get; set; }

        private readonly TwitchHttpClient _twitchHttpClient;
        private readonly TwitchOptions _twitchOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TwitchService> _logger;
        private readonly IStorageService _storageService;
        private readonly TableStorageOptions _tableStorageOptions;

        public TwitchService(IOptionsMonitor<TwitchOptions> twitchOptions, IOptionsMonitor<TableStorageOptions> tableStorageOptions, IHttpContextAccessor httpContextAccessor, TwitchHttpClient twitchHttpClient, IStorageService storageService, ILogger<TwitchService> logger)
        {
            _twitchOptions = twitchOptions.CurrentValue;
            _tableStorageOptions = tableStorageOptions.CurrentValue;
            _httpContextAccessor = httpContextAccessor;
            _twitchHttpClient = twitchHttpClient;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<List<string>> GetBannedList(List<string> channels)
        {
            if (channels.Count == 0)
            {
                return null;
            }

            // Call Twitch and get the list of banned us
            foreach (var channel in channels)
            {
                await LoadChannelData(channel);

                var user = TwitchUsers.FirstOrDefault(result =>
                    string.Equals(result.LoginName, channel, StringComparison.CurrentCultureIgnoreCase));
                var response = await _twitchHttpClient.GetBannedUsers(user.Id);
            }

            return null;
        }

        public async Task<bool> UpdateFollowerSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus)
        {
            // TODO: UPDATE THE UpdateFollowerSubscription METHOD TO USE EVENTSUB INSTEAD OF THE OLD WAY
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateStreamChangeSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus)
        {
            // TODO: UPDATE THE UpdateFollowerSubscription METHOD TO USE EVENTSUB INSTEAD OF THE OLD WAY
            return await Task.FromResult(true);
        }

        public async Task LoadChannelData(string channel = null)
        {
            _logger.LogFormattedMessage("Loading Twitch channel data");
            if (TwitchUsers == null)
            {
                TwitchUsers = new List<TwitchUser>();
            }

            List<string> channels;

            if (!string.IsNullOrEmpty(channel))
            {
                _logger.LogFormattedMessage($"Getting channel data for {channel}");
                channels = new List<string> { channel };
            }
            else
            {
                _logger.LogFormattedMessage("Getting channels from Storage");
                channels = await _storageService.GetTwitchChannels();
                if (channels.Count == 0)
                {
                    _logger.LogFormattedMessage("No data in storage. Getting channels from Config settings");
                    channels = _twitchOptions.Channels;
                    _logger.LogFormattedMessage("Completed getting channels from Config settings");
                }

                if (TwitchUsers.Count > 0)
                {
                    return;
                }
            }
            _logger.LogFormattedMessage("Completed getting channels from Storage");

            _logger.LogFormattedMessage("Getting channel data from Twitch");
            TwitchUsers.AddRange(await _twitchHttpClient.GetTwitchChannels(channels).ConfigureAwait(false));

            var ids = TwitchUsers.Where(x => string.Equals(channel, x.LoginName, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Id);
            await GetCurrentSubscriptions(ids);
            _logger.LogFormattedMessage("Completed logging channel data from Twitch");
        }

        public async Task GetCurrentSubscriptions(IEnumerable<string> channelIds)
        {
            var subscriptionEntries = await _twitchHttpClient.GetSubscriptionData(channelIds);

            foreach (var entry in subscriptionEntries)
            {
                var user = TwitchUsers.FirstOrDefault(x => string.Equals(entry.Key, x.Id));
                if (user != null)
                {
                    user.IsStreamSubscribed = (entry.Value & TwitchSubscriptionStatus.StreamSubscription) == TwitchSubscriptionStatus.StreamSubscription;
                    user.IsFollowSubscribed = (entry.Value & TwitchSubscriptionStatus.FollowerSubscription) == TwitchSubscriptionStatus.FollowerSubscription;
                }
            }
        }

        public Task LoadMonitoredChannels()
        {
            _logger.LogInformation("Getting list of monitored channels");
            if (MonitoredChannels != null && MonitoredChannels.Count > 0)
            {
                _logger.LogInformation("MonitoredChannels already has a value");
            }
            else
            {
                // TODO: Get this list of channels correctly;
                // Get channels from Config
                // Get Channels from Storage.
                // Union with Config channels.
                // Verify that channels are still valid Twitch channels,
                // then return the final resultset

                MonitoredChannels = _twitchOptions.Channels;
                _logger.LogInformation("MonitoredChannels has been set to a list of channels");
            }
            return Task.FromResult(MonitoredChannels);
        }

        public async Task SubscribeToChannelEvents()
        {
            if (MonitoredChannels == null)
            {
                await LoadMonitoredChannels();
            }
            
            // Subscribe to ban/unban events


            var isSuccessful = await _twitchHttpClient.SubscribeToChannelEvents(MonitoredChannels);
        }

        // TODO: Create a GetSubscriptionData method from Twitch that can be used on the Channel component
    }
}
