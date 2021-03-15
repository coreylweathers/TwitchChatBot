using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Hubs;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;

namespace TwitchChatBot.Client.Pages
{
    public partial class Bot
    {
        private readonly string UPDATE_PASSWORD = "UpdatePassword";

        [Inject]
        protected IStorageService StorageService { get; set; }
        [Inject]
        protected IOptionsMonitor<TableStorageOptions> TableOptionsMonitor { get; set; }
        [Inject]
        protected ILogger<Bot> Logger { get; set; }
        [Inject]
        protected IHubContext<ChatHub> HubContext { get; set; }

        protected DateTimeOffset UpdatedPasswordTime { get; set; }

        protected string PasswordText { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Logger.LogFormattedMessage("This is when this was created");
            UpdatedPasswordTime = DateTime.UtcNow;

            await Task.CompletedTask;
        }

        private async Task UpdatePassword()
        {
            Logger.LogFormattedMessage("Updating the password to the chatbot");
            // VALIDATE THAT THE TEXT IS ACTUALLY UPDATED 
            if (string.IsNullOrEmpty(PasswordText))
            {
                Logger.LogFormattedMessage($"{nameof(PasswordText)} does not have a value");
                await Task.CompletedTask;
            }

            // SEND REQUEST VIA SIGNALR TO BOT
            await HubContext.Clients.All.SendAsync(UPDATE_PASSWORD, PasswordText);

            Logger.LogFormattedMessage("Updated the password to the chatbot");
        }

        private void ClearPassword()
        {
            PasswordText = string.Empty;
        }
    }
}
