using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
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

        [CascadingParameter]
        public IModalService Modal { get; set; }

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

        private void OpenChannel(string channelLogin)
        {
            var parameters = new ModalParameters();
            parameters.Add(nameof(channelLogin), channelLogin);

            Modal.Show<Channel>(channelLogin, parameters);
        }

        public async Task StartSubscription(string channel = null)
        {
            Logger.LogFormattedMessage("Starting Subscription");
            IEnumerable<string> channels = string.IsNullOrEmpty(channel) ? TwitchOptions.CurrentValue.Channels.AsEnumerable() : new[] { channel };
            _followerSubscriptionFlag = await TwitchService.UpdateFollowerSubscription(channels, SubscriptionStatus.Subscribe);
            _streamSubscriptionFlag = await TwitchService.UpdateStreamChangeSubscription(channels, SubscriptionStatus.Subscribe);
            Logger.LogFormattedMessage($"Started Subscription");
        }

        public async Task StopSubscription(string channel = null)
        {
            Logger.LogFormattedMessage("Stopping Subscription");
            IEnumerable<string> channels = string.IsNullOrEmpty(channel) ? TwitchOptions.CurrentValue.Channels.AsEnumerable() : new[] { channel };
            await TwitchService.UpdateFollowerSubscription(channels, SubscriptionStatus.Unsubscribe);
            await TwitchService.UpdateStreamChangeSubscription(channels, SubscriptionStatus.Unsubscribe);
            Logger.LogFormattedMessage("Stopped Subscription");
        }
    }
}
