using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;

namespace TwitchChatBot.Client.Services
{
    public class TwitchHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly TwitchOptions _twitchOptions;
        private readonly OAuthOptions _oauthOptions;
        public TwitchHttpClient(HttpClient httpClient, IOptionsMonitor<TwitchOptions> twitchMonitor, IOptionsMonitor<OAuthOptions> oauthMonitor)
        {
            _httpClient = httpClient;
            _twitchOptions = twitchMonitor.CurrentValue;
            _oauthOptions = oauthMonitor.CurrentValue;
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _oauthOptions.ClientId);
        }

        public void SetAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException($"The {nameof(accessToken)} is null or empty");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<HttpStatusCode> UpdateSubscription(TwitchWebhookRequest request)
        {
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            if (string.IsNullOrEmpty(authHeader.ToString()))
            {
                throw new HttpRequestException("Calling Twitch API requires an Access Token. Set the access token first");
            }

            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.Default });
            var formData = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_twitchOptions.WebhookSubscriptionUrl, formData);
            return response.StatusCode;
        }

        public async Task<List<TwitchUser>> GetTwitchChannels(List<string> channels)
        {
            if (channels == null || channels.Count == 0)
            {
                await Task.FromException(new ArgumentException("Can't get Twitch channel data: Twitch Channels list is null or empty", nameof(channels)));
            }

            if (string.IsNullOrEmpty(_twitchOptions.UserUrl))
            {
                await Task.FromException(new ArgumentNullException("The Twitch User URL is null or empty in the appsettings.json", nameof(_twitchOptions.UserUrl)));
            }

            var uriBuilder = new UriBuilder(new Uri(_twitchOptions.UserUrl))
            {
                Query = string.Join("&", channels.Select(c => $"login={c}")).TrimStart('&')
            };


            var destination = uriBuilder.Uri;
            var response = await _httpClient.GetAsync(destination);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return JsonConvert.DeserializeObject<List<TwitchUser>>(json["data"].ToString());


        }
    }
}
