using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TwitchChatBot.CLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Start the bot
            var bot = new Bot(config);
            await bot.Initialize().ConfigureAwait(false);

            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}
