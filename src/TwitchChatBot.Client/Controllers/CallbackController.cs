using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TwitchChatBot.Client.Filters;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;

namespace TwitchChatBot.Client.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(TwitchRequestFilter))]
    public class CallbackController : ControllerBase
    {
        private readonly IOptionsMonitor<TwitchOptions> _twitchOptions;

        public CallbackController(IOptionsMonitor<TwitchOptions> twitchOptions)
        {
            _twitchOptions = twitchOptions;
        }

        // TO PICK UP WHERE WE LEFT OFF, add the options so we can read our super secret secret value, resubscribe to Twitch to get them to call the controller so we can test locally, and figure out why the Twitch call is not routing to the API controllers. 

        [HttpPost("{channel}")]
        public async Task<IActionResult> Post()
        {
            /*using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var requestBody = await reader.ReadToEndAsync();*/

            var response = await JsonSerializer.DeserializeAsync<TwitchEventSubResponse>(Request.Body);
            return Ok(response?.Challenge);
        }

    }
}
