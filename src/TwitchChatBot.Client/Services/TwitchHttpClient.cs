using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Models.Twitch;
using TwitchChatBot.Client.Models.Twitch.Enums;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TwitchChatBot.Client.Services
{
    public class TwitchHttpClient : ITwitchHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<TwitchOptions> _twitchMonitor;
        private readonly IOptionsMonitor<OAuthOptions> _oauthMonitor;
        public TwitchHttpClient(HttpClient httpClient, IOptionsMonitor<TwitchOptions> twitchMonitor, IOptionsMonitor<OAuthOptions> oauthMonitor)
        {
            _httpClient = httpClient;
            _twitchMonitor = twitchMonitor;
            _oauthMonitor = oauthMonitor;
        }

        public async Task<HttpStatusCode> UpdateSubscription(TwitchWebhookRequest request)
        {
            // TODO: Replace Json.NET with System.Text.Json
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.Default });
            var formData = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_twitchMonitor.CurrentValue.Urls.EventSubscriptionUrl, formData);
            return response.StatusCode;
        }

        public async Task<List<TwitchUser>> GetTwitchChannels(List<string> channelLogins)
        {
            if (channelLogins == null || channelLogins.Count == 0)
            {
                await Task.FromException(new ArgumentException("Can't get Twitch broadcasterId data: Twitch Channels list is null or empty", nameof(channelLogins)));
            }

            if (string.IsNullOrEmpty(_twitchMonitor.CurrentValue.Urls.UserUrl))
            {
                await Task.FromException(new ArgumentNullException("The Twitch User URL is null or empty in the appsettings.json", nameof(_twitchMonitor.CurrentValue.Urls.UserUrl)));
            }

            var uriBuilder = new UriBuilder(new Uri(string.Concat(_twitchMonitor.CurrentValue.Urls.ApiUrl,_twitchMonitor.CurrentValue.Urls.UserUrl)))
            {
                Query = string.Join("&", channelLogins.Select(c => $"login={c}")).TrimStart('&')
            };
            var destination = uriBuilder.Uri;
            var response = await _httpClient.GetAsync(destination);
            response.EnsureSuccessStatusCode();

            // TODO: Replace Json.NET with System.Text.Json
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return JsonConvert.DeserializeObject<List<TwitchUser>>(json["data"].ToString());

        }

        public async Task<Dictionary<string, TwitchSubscriptionStatus>> GetSubscriptionData(IEnumerable<string> channelIds)
        {
            
            // Get the JSON Response array and cache it locally
            var response = await _httpClient.GetAsync($"{_twitchMonitor.CurrentValue.Urls.SubscriptionUrl}?broadcaster_id={channelIds.FirstOrDefault()}");
            response.EnsureSuccessStatusCode();

            // TODO: Replace Json.NET with System.Text.Json
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            var subscriptions = JsonConvert.DeserializeObject<List<TwitchWebhookSubscriptionResponse>>(json["data"].ToString());
            // Using the TwitchUser list, match the userId to the broadcasterId login name
            // Check which topics are subscribed for that user ID if any
            // Return an enum that is the union of each topic
            var resultDictionary = new Dictionary<string, TwitchSubscriptionStatus>();
            foreach (var id in channelIds)
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
            return resultDictionary;
        }

        public async Task<bool> SubscribeToChannelEvents(List<string> channelLogins)
        {
            // TODO: Update the appsettings.json file with the right list of Twilio channels to monitor!!!
            var users = await GetTwitchChannels(channelLogins);
            
            foreach (var user in users)
            {
                var requestBody = new TwitchEventSubRequest 
                { 
                    Condition = new TwitchEventSubRequest.RequestCondition { BroadcasterUserId = user.Id },
                    Transport = new TwitchEventSubRequest.RequestTransport {
                        Callback = String.Format(_twitchMonitor.CurrentValue.CallbackTemplate, user.DisplayName),
                        Secret = _twitchMonitor.CurrentValue.WebhookSecret
                    } 
                };

                var json = System.Text.Json.JsonSerializer.Serialize<TwitchEventSubRequest>(requestBody, options: new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                var response = await _httpClient.PostAsJsonAsync("https://api.twitch.tv/helix/eventsub/subscriptions", requestBody, options: new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                var responseBody = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();
            }

            return true;
        }


        public async Task<TwitchBanResponse> GetBannedUsers(string broadcasterId)
        {
            var response = await _httpClient.GetAsync($"{_twitchMonitor.CurrentValue.Urls.BannedUsersUrl}?=broadcaster_id={broadcasterId}");
            response.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<TwitchBanResponse>(await response.Content.ReadAsStringAsync());

            // TODO: Parse out the list of banned users from the data object
            return data;
        }
    }
}
