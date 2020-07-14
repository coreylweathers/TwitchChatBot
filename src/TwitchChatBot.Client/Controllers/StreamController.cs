using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Hubs;
using TwitchChatBot.Client.Services;
using TwitchChatBot.Shared.Models.Entities;
using TwitchChatBot.Shared.Models.Enums;

namespace TwitchChatBot.Client.Controllers
{
    [ApiController]
    [Route("api/streams")]
    public class StreamController : ControllerBase
    {
        private const string TABLE_STREAMING = "streaming";
        private const string SIGNALR_UPDATECHANNEL = "UpdateChannelState";
        private readonly IStorageService _storageService;
        private readonly ILogger<StreamController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public StreamController(IStorageService storageService, ILogger<StreamController> logger, IHubContext<ChatHub> hubContext)
        {
            _storageService = storageService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost("subscription/{channel}")]
        public async Task<IActionResult> ProcessStreamEvent(string channel)
        {
            _logger.LogFormattedMessage($"Processing Twitch webhook for event on channel: {channel}");

            using var reader = new StreamReader(Request.Body);
            var messageText = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(messageText))
            {
                _logger.LogError("The message body is empty");
                return new NoContentResult();
            }

            // Check if the json["data"] has values. If so, join the Channel. If not, leave the channel
            var json = JObject.Parse(messageText);
            ChannelActivityEntity entity;

            if (json["data"].HasValues)
            {
                var startedAt = json["data"][0]["started_at"].ToString();
                var result = DateTime.TryParse(startedAt, out DateTime date);
                entity = new ChannelActivityEntity
                {
                    Activity = StreamActivity.StreamStarted.ToString(),
                    PartitionKey = channel,
                    RowKey = date.ToRowKeyString()
                };    
            }
            else
            {
                entity = new ChannelActivityEntity
                {
                    Activity = StreamActivity.StreamStopped.ToString(),
                    PartitionKey = channel,
                    RowKey = DateTime.UtcNow.ToRowKeyString()
                };
            }
            await _hubContext.Clients.All.SendAsync(SIGNALR_UPDATECHANNEL, entity);
            await _storageService.AddDataToStorage(entity, TABLE_STREAMING);
            _logger.LogFormattedMessage($"Completed processing Twitch webhook for event on channel: {channel}");
            return await Task.FromResult(NoContent());
        }

        [HttpGet("subscription/{channel}")]
        public async Task<IActionResult> ConfirmStreamSubscription([FromQuery(Name = "hub.challenge")] string challenge, string channel)
        {
            _logger.LogFormattedMessage($"Confirmed follower subscription for {channel} with challenge code {challenge}");
            return await Task.FromResult(new ContentResult { Content = challenge, ContentType = "text/plain", StatusCode = 200 });
        }
    }
}
