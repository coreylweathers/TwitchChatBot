using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;

namespace TwitchChatBot.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        //TODO: Enhance botcontroller by caching list of channels being monitored

        private readonly ILogger<BotController> _logger;
        private readonly IStorageService _storageService;
        private readonly IOptionsMonitor<TwitchOptions> _twitchOptionsMonitor;

        public BotController(ILogger<BotController> logger, IStorageService storageService, IOptionsMonitor<TwitchOptions> twitchOptionsMonitor)
        {
            _logger = logger;
            _storageService = storageService;
            _twitchOptionsMonitor = twitchOptionsMonitor;
        }

        [HttpPost("ban")]
        public IActionResult BanChannels([FromForm] List<string> channels)
        {
            if (channels.Count == 0)
            {
                return NoContent();
            }

            return NoContent();
            // The bot should not ban itself or the list of monitored channels

            // Load monitored channels from storage + config

            // From monitored channels get list of mods

            // The bot should have an easter egg response when it's time to ban phrakberg

            // If bot is in monitored channels or mods channels, do nothing

            // If bot isn't in monitored channels and mods channels, ban it


        }
    }
}
