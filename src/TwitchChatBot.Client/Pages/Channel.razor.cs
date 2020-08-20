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

        private bool _isChangeSaved { get; set; }

        private TwitchChannelModel model = new TwitchChannelModel();
        internal TwitchChannelModel initialState;

        private string _changesSavedStatus;

        protected override async Task OnInitializedAsync()
        {
            await TwitchService.GetChannelData(new List<string> { ChannelLogin });

            var channel = TwitchService.TwitchUsers.FirstOrDefault(x => string.Equals(x.LoginName, ChannelLogin, System.StringComparison.InvariantCultureIgnoreCase));
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
                _isChangeSaved = await TwitchService.UpdateFollowerSubscription(new[] { model.ChannelName }, model.IsFollowerSubscribed ? SubscriptionStatus.Subscribe : SubscriptionStatus.Unsubscribe) && 
                    await TwitchService.UpdateStreamChangeSubscription(new[] { model.ChannelName }, model.IsStreamSubscribed ? SubscriptionStatus.Subscribe : SubscriptionStatus.Unsubscribe);
                _changesSavedStatus = "Changes Saved!";
            }
            _isChangeSaved = true;
            StateHasChanged();
        }
    }
}
