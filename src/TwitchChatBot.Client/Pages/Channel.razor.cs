using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public string channelLogin { get; set; }

        private TwitchChannelModel model = new TwitchChannelModel();

        protected override async Task OnInitializedAsync()
        {
            await TwitchService.LoadChannelData(new List<string> { channelLogin });

            var channel = TwitchService.TwitchUsers.FirstOrDefault(x => string.Equals(x.LoginName, channelLogin, System.StringComparison.InvariantCultureIgnoreCase));
            model = new TwitchChannelModel
            {
                ChannelName = channel.DisplayName,
                ImageUrl = channel.ProfileImageUrl
            };

 
            // TODO: Call the TwitchService GetSubscription data for a specific channel and use the results to set the checkboxes for subscribed events correctly
        }

        private void SaveChanges()
        {

        }
    }
}
