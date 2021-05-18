using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace TwitchChatBot.Client.Services
{
    public class TwitchSubscriptionService : IHostedService
    {
        private readonly ITwitchService _twitchService;

        public TwitchSubscriptionService(ITwitchService twitchService)
        {
            _twitchService = twitchService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Subscribe to Twitch channel events for the list of monitored channels
            //var monitoredChannels = _twitchService.GetMonitoredChannels();
           //await _twitchService.SubscribeToChannelEvents();            
         }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }
    }
}