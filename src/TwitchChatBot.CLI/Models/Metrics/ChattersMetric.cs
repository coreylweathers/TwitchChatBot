using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchChatBot.Shared.Models.Entities;
using System.Linq;
using TwitchChatBot.Shared.Models.Enums;
using System;

namespace TwitchChatBot.CLI.Models.Metrics
{
    public class ChattersMetric : IMetric
    {
        public string Name { get; } = "Chatters";
        protected IReadOnlyCollection<ChannelActivityEntity> Results { get; set; }
        public ChattersMetric(IReadOnlyCollection<ChannelActivityEntity> results)
        {
            Results = results;
        }
        public Task<double> Calculate()
        {
            var chatMessages = Results.Where(x => string.Equals(x.Activity, StreamActivity.MessagePosted.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return Task.FromResult(Convert.ToDouble(chatMessages.Select(x => x.Viewer).Distinct().Count()));
        }
    }
}
