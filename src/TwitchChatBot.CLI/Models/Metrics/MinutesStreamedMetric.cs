using System;
using System.Threading.Tasks;

namespace TwitchChatBot.CLI.Models.Metrics
{
    public class MinutesStreamedMetric : IMetric
    {
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public string Name { get; } = "MinutesStreamed";

        public MinutesStreamedMetric(DateTime startTime, DateTime endTime)
        {
            if (startTime > endTime)
            {
                throw new ArgumentException("StartTime must be lower than EndTime");
            }
            StartTime = startTime;
            EndTime = endTime;
        }


        public Task<double> Calculate()
        {
            return Task.FromResult(EndTime.Subtract(StartTime).TotalMinutes);
        }
    }
}
