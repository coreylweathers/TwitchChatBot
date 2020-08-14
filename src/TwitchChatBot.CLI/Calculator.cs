using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Timers;
using TwitchChatBot.CLI.Models;
using TwitchChatBot.CLI.Models.Metrics;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Shared.Models.Entities;
using TwitchChatBot.Shared.Models.Enums;

namespace TwitchChatBot.CLI
{
    public class Calculator
    {
        private CloudTableClient _tableClient;
        private readonly IConfiguration _config;
        private readonly List<TwitchChannel> _channels;
        private readonly HttpClient _httpClient;
        public Calculator(IConfiguration config)
        {
            _config = config;
            _channels = new List<TwitchChannel>();
            _httpClient = new HttpClient();
        }

        public async Task InitializeCalculator()
        {
            _tableClient = await Common.CreateTableClient(Constants.CONFIG_TABLE_CONNECTIONSTRING);
        }

        public async Task StartViewerTimer(string channel)
        {
            // Start a timer for the channel
            if (!_channels.Any(x => string.Equals(x.Channel, channel, StringComparison.InvariantCultureIgnoreCase)))
            {
                var newChannel = new TwitchChannel
                {
                    Channel = channel,
                    Timer = CreateTimer(channel)
                };
                _channels.Add(newChannel);
            }
            await Task.CompletedTask;
        }

        public async Task StopViewerTimer(string channel)
        {
            var result = _channels.FirstOrDefault(x => string.Equals(x.Channel, channel, StringComparison.InvariantCultureIgnoreCase));
            result?.Timer.Stop();
            if (result != null)
            {
                var index = _channels.IndexOf(result);
                _channels.RemoveAt(index);
            }
            await Task.CompletedTask;
        }

        public async Task CalculateStreamStatistics(string channel, DateTime end)
        {
            var channelStartDate = end.Date;

            var table = _tableClient.GetTableReference(_config[Constants.CONFIG_TABLE_STREAMINGTABLE]);
            var streamResults = table.CreateQuery<ChannelActivityEntity>()
                .Where(x => x.PartitionKey == channel && x.Timestamp >= channelStartDate)
                .ToList();

            string temp;

            // 1 Identify stream start(startDate).
            // Use StreamStarted event or earliest messagePostedTime
            temp = streamResults.FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.StreamStarted.ToString(),StringComparison.InvariantCultureIgnoreCase)).RowKey ?? 
                streamResults.OrderBy(x => x.Timestamp).FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.MessagePosted.ToString(), StringComparison.InvariantCultureIgnoreCase)).RowKey;
            var startDate = DateTime.Parse(temp);

            // 2 Identify stream end(endDate)
            // Use StreamStopped event or latest messagePostedTime
            temp = streamResults.FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.StreamStopped.ToString(),
                StringComparison.InvariantCultureIgnoreCase)).RowKey ?? streamResults.OrderByDescending(x => x.Timestamp)
                .FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.MessagePosted.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)).RowKey;
            var endDate = DateTime.Parse(temp);

            // 3 Identify avg viewers (avgViewerCount) 

            // 4 Identify max number of viewers (maxViewerCount)
            /*var viewers = streamResults
                .Select(x => x.Viewer)
                .Where(x => !string.IsNullOrEmpty(x) && !string.Equals(x, channel, StringComparison.InvariantCultureIgnoreCase))
                .Distinct();
            var maxViewerCount = viewers.Count();*/

            // 11 Identify minutes streamed
            var minutesStreamedMetric = new MinutesStreamedMetric(startDate, endDate);
            var minutesStreamed = await minutesStreamedMetric.Calculate();

            /*var duration = new TimeSpan(0, 5, 0);
            var tempTime = startDate.Add(duration);
            var avgs = new List<int>();
            List<string> viewerList;
            while (tempTime <= endDate)
            {
                viewerList = streamResults
                    .Where(x => x.Timestamp >= startDate && x.Timestamp < tempTime)
                    .Where(x => x.Activity == StreamActivity.UserJoined.ToString() || x.Activity == StreamActivity.MessagePosted.ToString())
                    .Select(x => x.Viewer)
                    .Distinct().ToList();

                var exitList = streamResults
                    .Where(x => x.Timestamp >= startDate && x.Timestamp < tempTime)
                    .Where(x => x.Activity == StreamActivity.UserLeft.ToString())
                    .Select(x => x.Viewer)
                    .Distinct();

                var finalViewers = viewerList.Except(exitList);
                avgs.Add(finalViewers.Count());

                tempTime = tempTime.Add(duration);
            }

            var avgViewerCount = avgs.Average();*/

            // 5 Identify total views

            // 6 Identify unique views

            // 7 Identify minutes watched
            var minutesWatchedMetric = new MinutesWatchedMetric(channel, startDate, endDate, streamResults);
            var minutesWatched = await minutesWatchedMetric.Calculate();

            // 8 Identify new followers
            var newFollowerMetric = new NewFollowersMetric(streamResults);
            var newFollowers = await newFollowerMetric.Calculate();

            // 9 Identify chatter
            var chattersMetric = new ChattersMetric(streamResults);
            var chatters = await chattersMetric.Calculate();

            // 10 Identify Chat messages
            var chatMessagesMetric = new ChatMessagesMetric(streamResults);
            var chatMessageCount = await chatMessagesMetric.Calculate();

            // 12 Send data to the spreadsheet

            var httpClient = new HttpClient();
            var uri = new Uri(_config[Constants.CONFIG_ZAPIER_STREAMSUMMARYURL]);
            var data = new Dictionary<string, string>
            {
                {"startDate", startDate.ToString("ddd MMM dd yyyy")},
                {"endDate", endDate.ToString("ddd MMM dd yyyy") },
                {"minutesWatched", Convert.ToString(minutesWatched) },
                {"newFollowers", Convert.ToString(newFollowers)},
                {"chatter", Convert.ToString(chatters)},
                { "chatMessages", Convert.ToString(chatMessageCount)},
                {"minutesStreamed", Convert.ToString(minutesStreamed) },
                {"channel", channel }
            };

            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Saving data to Google sheet");
            var content = new FormUrlEncodedContent(data);
            try
            {
                var response = await httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Something went wrong", ex);
                throw;
            }
            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Saved Data to Google sheet");

        }

        private Timer CreateTimer(string channel)
        {
            var timer = new Timer
            {
                Interval = 5 * 60 * 100
            };
            timer.Elapsed += async (s, e) => await LogTwitchViewers(channel);
            timer.AutoReset = true;
            timer.Start();

            return timer;
        }

        private async Task LogTwitchViewers(string channel)
        {
            // NULL means channel offline; Value means channel is online
            (int? count, DateTime timeStamp) = await GetTwitchViewerCount(channel);

            if (!count.HasValue)
            {
                await Task.CompletedTask;
                return;
            }

            var entity = new ChannelViewersEntity
            {
                PartitionKey = channel,
                RowKey = DateTime.UtcNow.ToRowKeyString(),
                StartedAt = timeStamp,
                ViewerCount = count.Value
            };

            if (_tableClient == null)
            {
                _tableClient = await Common.CreateTableClient(_config[Constants.CONFIG_TABLE_CONNECTIONSTRING]);
            }
            await Common.AddEntityToStorage(_tableClient, entity, _config[Constants.CONFIG_TABLE_METRICSTABLE]);
        }

        private async Task<(int?, DateTime)> GetTwitchViewerCount(string channel)
        {
            bool isSuccess = false;
            int? viewerCount = null;
            DateTime timeStamp = default;
            while (!isSuccess)
            {
                var uriBuilder = new UriBuilder(_config[Constants.CONFIG_TWITCH_STREAMURI])
                {
                    Query = $"user_login={channel}"
                };

                var response = await _httpClient.GetAsync(uriBuilder.ToString());
                isSuccess = response.IsSuccessStatusCode;

                if (isSuccess == false)
                {
                    await SetAccessToken();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var dateHeader = response.Headers.FirstOrDefault(x => string.Equals(x.Key, "Date", StringComparison.InvariantCultureIgnoreCase));
                    timeStamp = DateTime.Parse(dateHeader.Value.SingleOrDefault());

                    var json = JObject.Parse(content);
                    if (json["data"].HasValues)
                    {
                        viewerCount = int.Parse(json["data"][0]["viewer_count"].ToString());
                    }
                }
            }

            return (viewerCount, timeStamp);
        }

        private async Task SetAccessToken()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("client_id", _config[Constants.CONFIG_TWITCH_CLIENTID]),
                new KeyValuePair<string, string>("client_secret", _config[Constants.CONFIG_TWITCH_CLIENTSECRET]),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var uri = new Uri(_config[Constants.CONFIG_TWITCH_TOKENURI]);
            var response = await _httpClient.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = json["access_token"].ToString();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _config[Constants.CONFIG_TWITCH_CLIENTID]);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
