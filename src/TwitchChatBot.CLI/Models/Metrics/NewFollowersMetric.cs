using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchChatBot.Shared.Models.Entities;
using System.Linq;
using TwitchChatBot.Shared.Models.Enums;

namespace TwitchChatBot.CLI.Models.Metrics
{
    public class NewFollowersMetric : IMetric
    {
        protected IReadOnlyCollection<ChannelActivityEntity> Results { get; set; }
        public string Name { get; } = "NewFollowers";

        public NewFollowersMetric(IReadOnlyCollection<ChannelActivityEntity> results)
        {
            Results = results;
        }
        public Task<double> Calculate()
        {
            var newFollowers = Results.Where(x => string.Equals(x.Activity, StreamActivity.UserFollowed.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return Task.FromResult(Convert.ToDouble(newFollowers.Count()));
        }
    }
}
