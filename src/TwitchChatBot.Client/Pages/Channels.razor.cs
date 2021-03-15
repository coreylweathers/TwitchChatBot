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
        protected IStorageService StorageService { get; set; }
        [Inject]
        protected ITwitchService TwitchService { get; set; }
        [Inject]
        protected ILogger<Channels> Logger { get; set; }
        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [CascadingParameter]
        public IModalService Modal { get; set; }

        private string _newChannelText;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity.IsAuthenticated)
            {
                await TwitchService.LoadChannelData();
            }
        }

        private void OpenChannel(string channelLogin)
        {
            var parameters = new ModalParameters();
            parameters.Add(nameof(channelLogin), channelLogin);

            Modal.Show<Channel>(channelLogin, parameters);
        }

        private async Task StartSubscription(string channel = null)
        {
            Logger.LogFormattedMessage("Starting Subscription");
            IEnumerable<string> channels = string.IsNullOrEmpty(channel) ? TwitchService.TwitchUsers.Select(x => x.LoginName) : new[] { channel };
            await TwitchService.UpdateFollowerSubscription(channels, SubscriptionStatus.Subscribed);
            await TwitchService.UpdateStreamChangeSubscription(channels, SubscriptionStatus.Subscribed);
            Logger.LogFormattedMessage($"Started Subscription");
        }

        private async Task StopSubscription(string channel = null)
        {
            Logger.LogFormattedMessage("Stopping Subscription");
            var channels = string.IsNullOrEmpty(channel) ? TwitchService.TwitchUsers.Select(x => x.LoginName) : new[] { channel };
            await TwitchService.UpdateFollowerSubscription(channels, SubscriptionStatus.Unsubscribed);
            await TwitchService.UpdateStreamChangeSubscription(channels, SubscriptionStatus.Unsubscribed);
            Logger.LogFormattedMessage("Stopped Subscription");
        }

        public async Task AddChannel()
        {
            if (string.IsNullOrEmpty(_newChannelText))
            {
                await Task.CompletedTask;
            }

            if(!TwitchService.TwitchUsers.Any(x => string.Equals(x.LoginName, _newChannelText, System.StringComparison.InvariantCultureIgnoreCase)))
            {
                await StartSubscription(_newChannelText);
            }
            _newChannelText = string.Empty;
            StateHasChanged();
        }
    }
}
