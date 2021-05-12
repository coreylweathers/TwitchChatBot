using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TwitchChatBot.Client.Models.Options;

namespace TwitchChatBot.Client.Filters
{
    public class TwitchRequestFilter : IAsyncActionFilter
    {
        private readonly IOptionsMonitor<TwitchOptions> _twitchOptions;

        public TwitchRequestFilter(IOptionsMonitor<TwitchOptions> twitchOptions)
        {
            _twitchOptions = twitchOptions;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            context.HttpContext.Request.EnableBuffering();
            
            var secret = _twitchOptions.CurrentValue.WebhookSecret;
            using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8);
            var requestBody = await reader.ReadToEndAsync();

            var sha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = Encoding.UTF8.GetBytes(string.Concat(
                context.HttpContext.Request.Headers["Twitch-Eventsub-Message-Id"],
                context.HttpContext.Request.Headers["Twitch-Eventsub-Message-Timestamp"],
                requestBody));

            var hash = sha256.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length);
            foreach (var t in hash)
            {
                sb.Append(t.ToString("x2"));
            }
            var hashString = sb.ToString();
            var messageSignature =context.HttpContext.Request.Headers["Twitch-Eventsub-Message-Signature"]; 
            if (!string.Equals(messageSignature, $"sha256={hashString}"))
            {
                context.Result = new ForbidResult();
            }

            // Reset the stream position to work in the action method
            context.HttpContext.Request.Body.Position = 0;
        }
    }
}