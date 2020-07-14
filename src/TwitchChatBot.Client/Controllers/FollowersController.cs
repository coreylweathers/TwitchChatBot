using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Client.Services;
using TwitchChatBot.Shared.Models.Entities;
using TwitchChatBot.Shared.Models.Enums;

namespace TwitchChatBot.Client.Controllers.Twitch
{
    [ApiController]
    [Route("api/followers")]
    public class FollowersController : ControllerBase
    {
        private readonly ILogger<FollowersController> _logger;
        private readonly IStorageService _storageService;
        private readonly TableStorageOptions _tableStorageOptions;

        public FollowersController(ILogger<FollowersController> logger, IStorageService storageService, IOptionsMonitor<TableStorageOptions> tableStorageOptionsMonitor)
        {
            _logger = logger;
            _storageService = storageService;
            _tableStorageOptions = tableStorageOptionsMonitor.CurrentValue;
        }

        [HttpPost("subscription/{channel}")]
        public async Task<IActionResult> ProcessFollowerEvent(string channel)
        {
            _logger.LogFormattedMessage($"Processing Twitch webhook for channel {channel}");
            using var reader = new StreamReader(Request.Body);
            var data = await reader.ReadToEndAsync();
            var json = JObject.Parse(data);
            var updates = json["data"].ToObject<List<TwitchWebhookFollowersResponse>>();

            try
            {
                foreach (var update in updates)
                {
                    var entity = new ChannelActivityEntity
                    {
                        Activity = StreamActivity.UserFollowed.ToString(),
                        PartitionKey = update.ToName,
                        RowKey = update.FollowedAt.ToRowKeyString(),
                        Viewer = update.FromName
                    };
                    var result = await _storageService.AddDataToStorage(entity, _tableStorageOptions.StreamingTable);
                    _logger.LogFormattedMessage($"Processed Twitch webhook for channel {channel}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception has occurred", ex);
            }

            return NoContent();
        }

        [HttpGet("subscription/{channel}")]
        public async Task<IActionResult> ConfirmFollowerSubscription([FromQuery(Name = "hub.challenge")] string challenge, string channel)
        {
            _logger.LogFormattedMessage($"Confirmed follower subscription for {channel} with challenge code {challenge}");
            return await Task.FromResult(new ContentResult { Content = challenge, ContentType = "text/plain", StatusCode = 200 });
        }
    }
}
