using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Client.Models.Twitch.Enums;
using TwitchChatBot.Shared.Models.Entities;

namespace TwitchChatBot.Client.Services
{
    public class TwitchService : ITwitchService
    {
        public List<TwitchUser> TwitchUsers { get; set; }
        public string UserAccessToken { get; set; }
        public string AppAccessToken { get; set; }

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

        public async Task<bool> UpdateFollowerSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus)
        {
            _logger.LogFormattedMessage($"Updating the Followers Subscription for the selected channels to {subscriptionStatus}");
            foreach (var channel in channels)
            {
                // MAKE THE WEBHOOK REQUEST TO TWITCH
                _logger.LogFormattedMessage($"Starting the follower subscription for {channel}");
                var selected = TwitchUsers?.FirstOrDefault(user => string.Equals(channel, user.LoginName, StringComparison.InvariantCultureIgnoreCase));

                if (selected == null)
                {
                    await LoadChannelData(channel);
                    selected = TwitchUsers?.FirstOrDefault(user => string.Equals(channel, user.LoginName, StringComparison.InvariantCultureIgnoreCase));
                }

                var request = new TwitchWebhookRequest
                {
                    Callback = string.Format(_twitchOptions.FollowerCallbackTemplate, _httpContextAccessor.HttpContext.Request.Host.Value, channel),
                    Mode = subscriptionStatus.ToString().ToLower(),
                    Topic = string.Format(_twitchOptions.FollowerTopicTemplate, selected.Id),
                    Lease = _twitchOptions.DefaultLease
                };

                // UPDATE THE SUBSCRIPTION STATUS IN STORAGE
                _logger.LogFormattedMessage($"Updating the follower subscription for {request}");
                var responseCode = await _twitchHttpClient.UpdateSubscription(request).ConfigureAwait(false);
                _logger.LogFormattedMessage($"Twitch response from updating subscription: {responseCode}");
                if (responseCode != HttpStatusCode.Accepted)
                {
                    _logger.LogWarning($"Unable to successfully subscribe to follower events for {request}");
                    return false;
                }
                selected.IsFollowSubscribed = subscriptionStatus == SubscriptionStatus.Subscribed;

                _logger.LogFormattedMessage("Adding Subscription Activity Update to storage");
                var entity = new SubscriptionActivityEntity
                {
                    PartitionKey = channel,
                    RowKey = DateTime.UtcNow.ToRowKeyString(),
                    Activity = "FollowSubscription",
                    State = subscriptionStatus.ToString()
                };
                var result = await _storageService.AddDataToStorage(entity, _tableStorageOptions.SubscriptionTable).ConfigureAwait(false);
                _logger.LogFormattedMessage("Added Subscription Activity Update to storage");
            }
            _logger.LogFormattedMessage($"Completed updating the follower Subscription for the selected channels to {subscriptionStatus}");
            return true;
        }

        public async Task<bool> UpdateStreamChangeSubscription(IEnumerable<string> channels, SubscriptionStatus subscriptionStatus)
        {
            _logger.LogFormattedMessage($"Updating the Stream Subscription for the selected channels to {subscriptionStatus}");
            foreach (var channel in channels)
            {
                _logger.LogFormattedMessage($"Starting the stream subscription for {channel}");
                var selected = TwitchUsers?.FirstOrDefault(user => string.Equals(channel, user.LoginName, StringComparison.InvariantCultureIgnoreCase));

                var request = new TwitchWebhookRequest
                {
                    Callback = string.Format(_twitchOptions.StreamChangeCallbackTemplate, _httpContextAccessor.HttpContext.Request.Host.Value, channel),
                    Mode = subscriptionStatus.ToString().ToLower(),
                    Topic = string.Format(_twitchOptions.StreamChangeTopicTemplate, selected.Id),
                    Lease = _twitchOptions.DefaultLease
                };

                _logger.LogFormattedMessage($"Updating the stream subscription for {request}");
                var responseCode = await _twitchHttpClient.UpdateSubscription(request).ConfigureAwait(false);
                if (responseCode != HttpStatusCode.Accepted)
                {
                    _logger.LogWarning($"Unable to successfully subscribe to stream events for {request}");
                    return false;
                }
                selected.IsStreamSubscribed = subscriptionStatus == SubscriptionStatus.Subscribed;

                _logger.LogFormattedMessage($"Updated the stream subscription successfully for {request}");

                _logger.LogFormattedMessage("Adding Subscription Activity Update to storage");
                var entity = new SubscriptionActivityEntity
                {
                    PartitionKey = channel,
                    RowKey = DateTime.UtcNow.ToRowKeyString(),
                    Activity = "StreamSubscription",
                    State = subscriptionStatus.ToString()
                };
                var result = await _storageService.AddDataToStorage(entity, _tableStorageOptions.SubscriptionTable).ConfigureAwait(false);
                _logger.LogFormattedMessage("Added Subscription Activity Update to storage");
            }
            _logger.LogFormattedMessage($"Completed updating the Stream Subscription for the selected channels to {subscriptionStatus}");
            return true;
        }

        public async Task LoadChannelData(string channel = null)
        {
            _logger.LogFormattedMessage("Loading Twitch channel data");
            if (TwitchUsers == null)
            {
                TwitchUsers = new List<TwitchUser>();
            }

            if (!string.IsNullOrEmpty(channel) && TwitchUsers.Any(x => string.Equals(x.LoginName, channel, StringComparison.InvariantCultureIgnoreCase)))
            {
                await Task.CompletedTask;
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
                
                foreach(var entry in channels)
                {
                    if (!TwitchUsers.Any(x => string.Equals(x.LoginName, entry, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        TwitchUsers.Clear();
                    }
                }

                if (TwitchUsers.Count > 0)
                {
                    return;
                }
            }
            _logger.LogFormattedMessage("Completed getting channels from Storage");

            _logger.LogFormattedMessage("Getting channel data from Twitch");
            var accessTokenClaim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => string.Equals(x.Type, "idp_access_token", StringComparison.InvariantCultureIgnoreCase));
            _twitchHttpClient.SetUserAccessToken(accessTokenClaim.Value);
            TwitchUsers.AddRange(await _twitchHttpClient.GetTwitchChannels(channels).ConfigureAwait(false));

            var ids = TwitchUsers.Where(x => string.Equals(channel,x.LoginName, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Id);
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

        public Task SetUserAccessToken(string token)
        {
            _twitchHttpClient.SetUserAccessToken(token);
            UserAccessToken = token;
            return Task.CompletedTask;
        }

        public async Task SetAppAccessToken(string clientId, string clientSecret)
        {
            AppAccessToken = await _twitchHttpClient.GetAppAccessToken(clientId, clientSecret);
        }

        // TODO: Create a GetSubscriptionData method from Twitch that can be used on the Channel component
    }
}
