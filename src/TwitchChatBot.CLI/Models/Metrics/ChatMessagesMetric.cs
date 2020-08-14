using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TwitchChatBot.CLI.Models.Metrics
{
    public class ChatMessagesMetric : IMetric
    {
        public string Name { get; } = "ChatMessages";
        private IReadOnlyCollection<object> Messages { get; }

        public ChatMessagesMetric( IReadOnlyCollection<object> messages)
        {
            Messages = messages;
        }

        public Task<double> Calculate()
        {
            return Task.FromResult(Convert.ToDouble(Messages.Count));
        }
    }
}
