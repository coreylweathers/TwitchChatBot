using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Shared.Models.Entities;

namespace TwitchChatBot.Client.Services
{
    public class TwitchService : ITwitchService
    {
        public List<TwitchUser> TwitchUsers { get; set; }
        public string AccessToken { get; set; }

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

        public async Task<bool> UpdateFollowerSubscription(List<string> channels, SubscriptionStatus subscriptionStatus)
        {
            _logger.LogFormattedMessage($"Updating the Followers Subscription for the selected channels to {subscriptionStatus}");
            foreach (var channel in channels)
            {
                // MAKE THE WEBHOOK REQUEST TO TWITCH
                _logger.LogFormattedMessage($"Starting the follower subscription for {channel}");
                var selected = TwitchUsers?.FirstOrDefault(user => string.Equals(channel, user.LoginName, StringComparison.InvariantCultureIgnoreCase));

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
                selected.IsFollowSubscribed = subscriptionStatus == SubscriptionStatus.Subscribe;

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

        public async Task<bool> UpdateStreamChangeSubscription(List<string> channels, SubscriptionStatus subscriptionStatus)
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
                selected.IsStreamSubscribed = subscriptionStatus == SubscriptionStatus.Subscribe;

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

        public async Task LoadChannelData(List<string> channels)
        {
            _logger.LogFormattedMessage("Loading Twitch channel data");
            if (TwitchUsers != null && TwitchUsers.Count > 0)
            {
                _logger.LogFormattedMessage("Twitch channel data has already been loaded");
                await Task.CompletedTask;
                return;
            }
            else if (TwitchUsers == null)
            {
                TwitchUsers = new List<TwitchUser>();
            }

            _logger.LogFormattedMessage("Getting channel data from Twitch");
            TwitchUsers.AddRange(await _twitchHttpClient.GetTwitchChannels(channels).ConfigureAwait(false));
            _logger.LogFormattedMessage("Completed logging channel data from Twitch");
        }

        public async Task GetCurrentSubscriptions()
        {
            // check subscriptions table and get status
            foreach (var user in TwitchUsers)
            {
                _ = await _storageService.GetSubscriptionStatus(user.LoginName).ConfigureAwait(false);
            }
            await Task.CompletedTask;
        }

        public Task UnsubscribeFromChannelEvents(List<string> channel)
        {
            throw new NotImplementedException();
        }

        public Task SetAccessToken(string token)
        {
            _twitchHttpClient.SetAccessToken(token);
            AccessToken = token;
            return Task.CompletedTask;
        }

    }
}
