using System.Threading.Tasks;

namespace TwitchChatBot.CLI.Models.Metrics
{
    public interface IMetric
    {
        string Name { get; }
        Task<double> Calculate();
    }
}
