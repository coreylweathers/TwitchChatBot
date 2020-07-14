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
            var index = _channels.IndexOf(result);
            _channels.RemoveAt(index);
            await Task.CompletedTask;
        }

        public async Task CalculateStreamStatistics(string channel, DateTime end)
        {
            //var channelStartDate = DateTime.Parse("20200622");
            var channelStartDate = end.Date;

            var table = _tableClient.GetTableReference(_config[Constants.CONFIG_TABLE_METRICSTABLE]);
            var query = table.CreateQuery<ChannelActivityEntity>()
                .Where(x => x.PartitionKey == channel && x.Timestamp >= channelStartDate);

            var results = query.ToList();
            string temp;

            // 1 Identify stream start(startDate).
            // Use StreamStarted event or earliest messagePostedTime
            temp = results.FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.StreamStarted.ToString(),
                StringComparison.InvariantCultureIgnoreCase)).RowKey ?? results.OrderBy(x => x.Timestamp)
                .FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.MessagePosted.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)).RowKey;

            //temp = results.Where(x => string.Equals(x.Activity, StreamActivity.StreamStarted.ToString(), StringComparison.InvariantCultureIgnoreCase)).OrderBy(x => x.Timestamp).FirstOrDefault().RowKey;
            var startDate = DateTime.Parse(temp);

            // 2 Identify stream end(endDate)
            // Use StreamStopped event or latest messagePostedTime
            //temp = results.OrderByDescending(x => x.Timestamp).FirstOrDefault().RowKey;
            temp = results.FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.StreamStopped.ToString(),
                StringComparison.InvariantCultureIgnoreCase)).RowKey ?? results.OrderByDescending(x => x.Timestamp)
                .FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.MessagePosted.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)).RowKey;
            var endDate = DateTime.Parse(temp);

            // 3 Identify avg viewers (avgViewerCount) / max number of viewers (viewerCount) / number of minutesStreamed (minutesStreamed)
            // 4 Identify max number of viewers (maxViewerCount)
            // 11 Idenfity minutes streamed
            var viewers = results
                .Select(x => x.Viewer)
                .Where(x => !string.IsNullOrEmpty(x) && !string.Equals(x, channel, StringComparison.InvariantCultureIgnoreCase))
                .Distinct();
            var maxViewerCount = viewers.Count();
            var minutesStreamed = endDate.Subtract(startDate).TotalMinutes;
            //var avgViewerCount = maxViewerCount / minutesStreamed;

            var duration = new TimeSpan(0, 5, 0);
            var tempTime = startDate.Add(duration);
            var avgs = new List<int>();
            List<string> viewerList;
            while (tempTime <= endDate)
            {
                viewerList = results
                    .Where(x => x.Timestamp >= startDate && x.Timestamp < tempTime)
                    .Where(x => x.Activity == StreamActivity.UserJoined.ToString() || x.Activity == StreamActivity.MessagePosted.ToString())
                    .Select(x => x.Viewer)
                    .Distinct().ToList();

                var exitList = results
                    .Where(x => x.Timestamp >= startDate && x.Timestamp < tempTime)
                    .Where(x => x.Activity == StreamActivity.UserLeft.ToString())
                    .Select(x => x.Viewer)
                    .Distinct();

                var finalViewers = viewerList.Except(exitList);
                avgs.Add(finalViewers.Count());

                tempTime = tempTime.Add(duration);
            }

            var avgViewerCount = avgs.Average();

            // 5 Identify total views

            // 6 Identify unique views

            // 7 Identify minutes watched
            var minutesWatchedLookup = new Dictionary<string, List<TimeSpan>>();
            foreach (var viewer in viewers)
            {
                var events = results
                    .Where(x => string.Equals(viewer, x.Viewer, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(x => x.Timestamp)
                    .ToList();

                var dateValue = events.FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.UserJoined.ToString())) ??
                        events.OrderBy(x => x.Timestamp).FirstOrDefault();

                DateTime viewerStartDate = startDate;
                if (dateValue != null && !string.IsNullOrEmpty(dateValue.RowKey))
                {
                    viewerStartDate = DateTime.Parse(dateValue.RowKey);
                }

                // Add users who may not have left before the stream ended
                if (!events.Any(x => x.Activity == StreamActivity.UserLeft.ToString()))
                {
                    var tempMinutesWatched = endDate.Subtract(viewerStartDate);
                    minutesWatchedLookup.Add(viewer, new List<TimeSpan> { tempMinutesWatched });
                }
                else
                {
                    var userActivities = events
                        .OrderBy(x => x.Timestamp)
                        .Where(x => string.Equals(x.Activity, StreamActivity.UserJoined.ToString()) || string.Equals(x.Activity, StreamActivity.UserLeft.ToString()))
                        .Select(x => new
                        {
                            Activity = (StreamActivity)Enum.Parse(typeof(StreamActivity), x.Activity),
                            Timestamp = DateTime.Parse(x.RowKey)
                        })
                        .ToList();

                    // if first event if UserLeft, set the userStart to the streamstart
                    var firstActivity = userActivities.FirstOrDefault();
                    if (firstActivity != null && firstActivity.Activity == StreamActivity.UserLeft)
                    {
                        var difference = userActivities.First().Timestamp.Subtract(startDate);
                        if (minutesWatchedLookup.ContainsKey(viewer))
                        {
                            var currentMinutesWatchedList = minutesWatchedLookup[viewer];
                            currentMinutesWatchedList.Add(difference);
                        }
                        else
                        {
                            minutesWatchedLookup.Add(viewer, new List<TimeSpan> { difference });
                        }
                        userActivities.Remove(firstActivity);
                    }

                    // Finds all userJoined events and their closest corresponding UserLeft events
                    foreach (var entry in userActivities.Where(x => x.Activity == StreamActivity.UserJoined))
                    {
                        var exit = userActivities.FirstOrDefault(x => x.Activity == StreamActivity.UserLeft && x.Timestamp >= entry.Timestamp);
                        if (exit == null)
                        {
                            exit = new
                            {
                                Activity = StreamActivity.UserLeft,
                                Timestamp = endDate
                            };
                        }
                        var difference = exit.Timestamp.Subtract(entry.Timestamp);

                        if (minutesWatchedLookup.ContainsKey(viewer))
                        {
                            var currentMinutesWatchedList = minutesWatchedLookup[viewer];
                            currentMinutesWatchedList.Add(difference);
                        }
                        else
                        {
                            minutesWatchedLookup.Add(viewer, new List<TimeSpan> { difference });
                        }
                    }

                }
            }
            var minutesWatched = minutesWatchedLookup.Values.Sum(x => x.Sum(y => y.TotalMinutes));

            // 8 Identify new followers
            var newFollowers = results.Where(x => string.Equals(x.Activity, StreamActivity.UserFollowed.ToString(), StringComparison.InvariantCultureIgnoreCase));
            var followerCount = newFollowers.Count();

            // 9 Identify chatter
            var chatMessages = results.Where(x => string.Equals(x.Activity, StreamActivity.MessagePosted.ToString(), StringComparison.InvariantCultureIgnoreCase));
            var chatterCount = chatMessages.Select(x => x.Viewer).Distinct().Count();

            // 10 Identify Chat messages
            var chatMessageCount = chatMessages.Count();

            // 12 Send data to the spreadsheet

            var httpClient = new HttpClient();
            var uri = new Uri(_config[Constants.CONFIG_ZAPIER_STREAMSUMMARYURL]);
            var data = new Dictionary<string, string>
            {
                {"startDate", startDate.ToString(Constants.DATETIME_FORMAT)},
                {"endDate", endDate.ToString(Constants.DATETIME_FORMAT) },
                {"avgViewer", Convert.ToString(avgViewerCount) },
                {"maxViewer",Convert.ToString(maxViewerCount) },
                {"minutesWatched", Convert.ToString(minutesWatched) },
                {"newFollowers", Convert.ToString(followerCount)},
                {"chatter", Convert.ToString(chatterCount)},
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
                Interval = 1 * 60 * 100
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
