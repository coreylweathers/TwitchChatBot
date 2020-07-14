using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models;

namespace TwitchChatBot.Client.Extensions
{
    public static class Extensions
    {
        public static void LogFormattedMessage(this ILogger logger, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            logger.LogInformation($"{DateTime.UtcNow}: {message}");
        }

        public static string ToRowKeyString(this DateTime date)
        {
            return date.ToString("O");
            //return date.ToString("o").Replace(":", string.Empty).Replace("-", string.Empty).Replace(".", string.Empty);
        }
    }
}
