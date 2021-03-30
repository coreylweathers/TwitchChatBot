using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Shared.Models.Entities;

namespace TwitchChatBot.CLI.Models
{
    public class ZapierClient
    {
        private Dictionary<string, Uri> _urlDictionary { get; set; }
        private readonly HttpClient _httpClient;

        public ZapierClient()
        {

            _urlDictionary = new Dictionary<string, Uri>();
            _httpClient = new HttpClient();
        }

        public async Task AddUrl(string key, string url)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("The parameter specified is null or empty", nameof(key));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("The parameter specified is null or empty", nameof(url));
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("The Url passed along is not a valid Url. Please double check the value", nameof(url));
            }

            if (_urlDictionary.ContainsKey(key))
            {
                var value = _urlDictionary[key];
                if (!string.Equals(value.ToString(), url, StringComparison.InvariantCultureIgnoreCase))
                {
                    _urlDictionary[key] = new Uri(url);
                }
            }
            else
            {
                _urlDictionary.Add(key, new Uri(url));
            }

            await Task.CompletedTask;
            return;
        }

        public async Task<HttpStatusCode> AddChannelEvent(string eventType, ChannelActivityEntity entity)
        {
            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            if (!_urlDictionary.ContainsKey(eventType))
            {
                throw new ArgumentException($"The url for {eventType} is not available. Please update your appsettings and try again", nameof(eventType));
            }
            var postData = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string,string>("viewer", entity.Viewer),
                new KeyValuePair<string, string>("activity", entity.Activity),
                new KeyValuePair<string, string>("channel", entity.PartitionKey),
                new KeyValuePair<string, string>("timestamp", entity.Timestamp.DateTime.ToRowKeyString())
            });

            var uri = _urlDictionary[eventType];
            var response = await _httpClient.PostAsync(uri, postData);
            response.EnsureSuccessStatusCode();

            return response.StatusCode;
        }
    }
}
