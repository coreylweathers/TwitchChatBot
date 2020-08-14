using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;

namespace TwitchChatBot.Client.Pages
{
    public partial class Bot
    {
        [Inject]
        protected IStorageService StorageService { get; set; }
        [Inject]
        protected IOptionsMonitor<TableStorageOptions> TableOptionsMonitor { get; set; }
        [Inject]
        protected ILogger<Bot> Logger { get; set; }

        protected DateTimeOffset UpdatedPasswordTime { get; set; }

        protected string PasswordText { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Logger.LogFormattedMessage("This is when this was created");
            UpdatedPasswordTime = DateTime.UtcNow;

            await Task.CompletedTask;
        }
    }
}
