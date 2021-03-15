using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models;
using TwitchChatBot.Client.Models.Component;
using TwitchChatBot.Client.Services;

namespace TwitchChatBot.Client.Pages
{
    public partial class Channel
    {
        [CascadingParameter]
        public BlazoredModalInstance BlazoredModal { get; set; }

        [Inject]
        protected ITwitchService TwitchService { get; set; }

        [Parameter]
        public string ChannelLogin { get; set; }

        private bool IsChangeSaved { get; set; }

        private TwitchChannelModel model = new TwitchChannelModel();
        internal TwitchChannelModel initialState;

        private string _changesSavedStatus;

        protected override async Task OnInitializedAsync()
        {
            var channel = TwitchService.TwitchUsers.FirstOrDefault(x => string.Equals(x.LoginName, ChannelLogin, System.StringComparison.InvariantCultureIgnoreCase));

            if (channel == null)
            {
                await TwitchService.LoadChannelData(ChannelLogin);
            }

            model = new TwitchChannelModel
            {
                ChannelName = channel.DisplayName,
                ImageUrl = channel.ProfileImageUrl,
                IsFollowerSubscribed = channel.IsFollowSubscribed,
                IsStreamSubscribed = channel.IsStreamSubscribed
            };

            initialState = model;
        }

        private async Task SaveChanges()
        {
            if (initialState.Equals(model))
            {
                _changesSavedStatus = "No updates needed!";
            }
            else
            {
                IsChangeSaved = await TwitchService.UpdateFollowerSubscription(new[] { model.ChannelName }, model.IsFollowerSubscribed ? SubscriptionStatus.Subscribed : SubscriptionStatus.Unsubscribed) && 
                    await TwitchService.UpdateStreamChangeSubscription(new[] { model.ChannelName }, model.IsStreamSubscribed ? SubscriptionStatus.Subscribed : SubscriptionStatus.Unsubscribed);
                _changesSavedStatus = "Changes Saved!";
            }
            IsChangeSaved = true;
            StateHasChanged();
        }
    }
}
