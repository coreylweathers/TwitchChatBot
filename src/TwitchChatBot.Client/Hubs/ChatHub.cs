using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TwitchChatBot.Shared.Models.Enums;

namespace TwitchChatBot.Client.Hubs
{
    public class ChatHub : Hub
    {

        public async Task UpdateChannelState(string channel, StreamActivity activity)
        {
            await Clients.All.SendAsync("UpdateChannelState", channel, activity);
        }
    }
}
