using Microsoft.Extensions.Logging;
using System;

namespace TwitchChatBot.Shared.Extensions
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
            return date.ToString("O").Replace(":", string.Empty).Replace("-", string.Empty).Replace(".", string.Empty);
        }
    }
}
