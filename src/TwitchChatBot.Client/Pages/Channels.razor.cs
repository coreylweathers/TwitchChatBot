﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;

namespace TwitchChatBot.Client.Pages
{
    public partial class Channels
    {
        [Inject]
        protected IOptionsMonitor<TwitchOptions> TwitchOptions { get; set; }
        [Inject]
        protected ITwitchService TwitchService { get; set; }
        [Inject]
        protected ILogger<Channels> Logger { get; set; }
        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        // TODO: Move the buttons to be on each channel, and then setup a global button handler
        // TODO: Update button enabled state based on channel subscription status
        // TODO: Add subscription date to the screen
        private bool _followerSubscriptionFlag;
        private bool _streamSubscriptionFlag;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity.IsAuthenticated)
            {
                await TwitchService.LoadChannelData(TwitchOptions.CurrentValue.Channels);
                //await TwitchService.GetCurrentSubscriptions();
            }
        }

        public async Task StartSubscription()
        {
            Logger.LogFormattedMessage("Starting Subscription");
            _followerSubscriptionFlag = await TwitchService.UpdateFollowerSubscription(TwitchOptions.CurrentValue.Channels, SubscriptionStatus.Subscribe);
           _streamSubscriptionFlag =  await TwitchService.UpdateStreamChangeSubscription(TwitchOptions.CurrentValue.Channels, SubscriptionStatus.Subscribe);
            Logger.LogFormattedMessage($"Started Subscription");
        }

        public async Task StopSubscription()
        {
            Logger.LogFormattedMessage("Stopping Subscription");

            await TwitchService.UpdateFollowerSubscription(TwitchOptions.CurrentValue.Channels, SubscriptionStatus.Unsubscribe);
            await TwitchService.UpdateStreamChangeSubscription(TwitchOptions.CurrentValue.Channels, SubscriptionStatus.Unsubscribe);
            Logger.LogFormattedMessage("Stopped Subscription");
        }
    }
}
