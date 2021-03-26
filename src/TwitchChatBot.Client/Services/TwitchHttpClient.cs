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
using TwitchChatBot.Client.Models.Twitch.Enums;

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

        public async Task<string> GetAppAccessToken(string clientId, string clientSecret)
        {
            var originalAuthHeader = _httpClient.DefaultRequestHeaders.Authorization;
            _httpClient.DefaultRequestHeaders.Clear();

            var uriBuilder = new UriBuilder("https://id.twitch.tv/oauth2/token");

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("client_id={0}", clientId);
            stringBuilder.AppendFormat("&client_secret={0}", clientSecret);
            stringBuilder.AppendFormat("&grant_type=client_credentials");
            stringBuilder.AppendFormat($"&scopes=channel:read:subscription");
            uriBuilder.Query = stringBuilder.ToString();

            var response = await _httpClient.PostAsync(uriBuilder.Uri,null);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = json["access_token"].ToString();
            _httpClient.DefaultRequestHeaders.Authorization = originalAuthHeader;
            return accessToken;
        }

        public void SetUserAccessToken(string accessToken)
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

        public async Task<List<TwitchUser>> GetTwitchChannels(List<string> channelLogins)
        {
            if (channelLogins == null || channelLogins.Count == 0)
            {
                await Task.FromException(new ArgumentException("Can't get Twitch channel data: Twitch Channels list is null or empty", nameof(channelLogins)));
            }

            if (string.IsNullOrEmpty(_twitchOptions.UserUrl))
            {
                await Task.FromException(new ArgumentNullException("The Twitch User URL is null or empty in the appsettings.json", nameof(_twitchOptions.UserUrl)));
            }

            var uriBuilder = new UriBuilder(new Uri(_twitchOptions.UserUrl))
            {
                Query = string.Join("&", channelLogins.Select(c => $"login={c}")).TrimStart('&')
            };


            var destination = uriBuilder.Uri;
            var response = await _httpClient.GetAsync(destination);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return JsonConvert.DeserializeObject<List<TwitchUser>>(json["data"].ToString());

        }

        public async Task<Dictionary<string,TwitchSubscriptionStatus>> GetSubscriptionData(IEnumerable<string> channelIds)
        {
            // TODO: WRITE THE GETSUBSCRIPTION DATA METHOD FOR THE HTTP CLIENT
            // Make the request with the Client ID and Bearer Token set in headers
            var originalAuthValue = _httpClient.DefaultRequestHeaders.Authorization;

            var bearerToken = await GetAppAccessToken(_oauthOptions.ClientId, _oauthOptions.ClientSecret);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            // Get the JSON Response array and cache it locally
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _oauthOptions.ClientId);
            var uriBuilder = new UriBuilder(new Uri(_twitchOptions.WebhookSubscriptionsApiUrl))
            {
                Query = "first=100"
            };
            var response = await _httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            var subscriptions = JsonConvert.DeserializeObject<List<TwitchWebhookSubscriptionResponse>>(json["data"].ToString());
            // Using the TwitchUser list, match the userId to the channel login name
            // Check which topics are subscribed for that user ID if any
            // Return an enum that is the union of each topic
            var resultDictionary = new Dictionary<string, TwitchSubscriptionStatus>();
            foreach(var id in channelIds)
            {
                var subscriptionStatus = TwitchSubscriptionStatus.None;
                var results = subscriptions.Where(x => x.Topic.Query.EndsWith(id.ToString()));

                if (results.Count() == 2)
                {
                    subscriptionStatus = TwitchSubscriptionStatus.FollowerSubscription | TwitchSubscriptionStatus.StreamSubscription;
                }
                else
                {
                    var subscription = results.FirstOrDefault();
                    if (subscription != null)
                    {
                        subscriptionStatus = subscription.Topic.OriginalString.Contains("follows") ? TwitchSubscriptionStatus.FollowerSubscription : TwitchSubscriptionStatus.StreamSubscription;
                    }
                }

                resultDictionary.Add(id, subscriptionStatus);
            }

            _httpClient.DefaultRequestHeaders.Authorization = originalAuthValue;
            return resultDictionary;
        }
    }
}
