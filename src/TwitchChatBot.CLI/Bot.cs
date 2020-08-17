using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TwitchChatBot.CLI.Models;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Shared.Models.Entities;
using TwitchChatBot.Shared.Models.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace TwitchChatBot.CLI
{
    public class Bot
    {
        private readonly IConfiguration _config;
        private ZapierClient _zapierClient;
        private TwitchClient _twitchClient;
        private CloudTableClient _tableClient;
        private readonly string _streamingTable;
        private readonly Calculator _calculator;
        public Bot(IConfiguration config)
        {
            _config = config;
            _streamingTable = _config[Constants.CONFIG_TABLE_STREAMINGTABLE];
            _calculator = new Calculator(_config);
        }

        public async Task Initialize()
        {
            await SetupTwitchClient().ConfigureAwait(false);
            _tableClient = await Common.CreateTableClient(_config[Constants.CONFIG_TABLE_CONNECTIONSTRING]).ConfigureAwait(false);
            await SetupSignalR().ConfigureAwait(false);
            await SetupZapierClient().ConfigureAwait(false);
        }

        private async Task SetupSignalR()
        {
            var uri = new Uri($"{_config[Constants.CONFIG_SIGNALR_URI]}");
            var _hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(uri)
                .Build();

            _hubConnection.On<ChannelActivityEntity>("UpdateChannelState", async entity =>
            {
                var activity = (StreamActivity)Enum.Parse(typeof(StreamActivity), entity.Activity);
                switch (activity)
                {
                    case StreamActivity.StreamStarted:
                        await Start(entity.PartitionKey);
                        break;
                    case StreamActivity.StreamStopped:
                        await Stop(entity.PartitionKey);
                        break;
                    default:
                        break;
                }
            });

            _hubConnection.On<string>("UpdatePassword", async passwordText =>
            {
                await UpdatePassword(passwordText);
            });
            await _hubConnection.StartAsync().ConfigureAwait(false);
            Console.WriteLine($"{DateTime.UtcNow}: Connected to SignalR hub {uri} - {_hubConnection.ConnectionId}");
        }

        private async Task UpdatePassword(string password)
        {
            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Updating the Twitch bot password");
            var creds = new ConnectionCredentials(_config[Constants.CONFIG_TWITCH_USERNAME], password);

            try
            {
                // IF CONNECTED, DISCONNECT
                if (_twitchClient.IsConnected)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString()}: Disconnecting from Twitch");
                    _twitchClient.Disconnect();
                    Console.WriteLine($"{DateTime.UtcNow.ToString()}: Disconnected from Twitch");
                }

                // UPDATE THE CLIENT CREDS
                _twitchClient.SetConnectionCredentials(creds);

                // RECONNECT TO TWITCH
                _twitchClient.Reconnect();
                Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Updated the Twitch bot password");

            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occured: {0}", ex.Message);
            }
            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Updated the Twitch bot password");
            await Task.CompletedTask;
        }

        private async Task SetupTwitchClient()
        {
            var credentials =
                new ConnectionCredentials(_config[Constants.CONFIG_TWITCH_USERNAME], _config[Constants.CONFIG_TWITCH_PASSWORD]);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            var customClient = new WebSocketClient(clientOptions);
            _twitchClient = new TwitchClient(customClient);
            _twitchClient.Initialize(credentials);
            _twitchClient.AutoReListenOnException = true;

            _twitchClient.OnLog += Client_OnLog;
            _twitchClient.OnJoinedChannel += Client_OnJoinedChannel;
            _twitchClient.OnConnected += Client_OnConnected;
            _twitchClient.OnDisconnected += Client_OnDisconnected;
            _twitchClient.OnLeftChannel += async(s,e) => await Client_OnLeftChannel(s,e);
            _twitchClient.OnMessageReceived += async (s, e) => await Client_OnMessageReceived(s, e);
            _twitchClient.OnNewSubscriber += async (s, e) => await Client_OnNewSubscriber(s, e);
            _twitchClient.OnUserJoined += async (s, e) => await Client_OnUserJoined(s, e);
            _twitchClient.OnUserLeft += async (s, e) => await Client_OnUserLeft(s, e);
            _twitchClient.OnExistingUsersDetected += async (s, e) => await Client_OnExisingUsersDetected(s, e);

            _twitchClient.Connect();

            await Task.CompletedTask;
        }

        private async Task SetupZapierClient()
        {
            _zapierClient = new ZapierClient();
            await _zapierClient.AddUrl(Constants.ZAPIER_EVENTTYPE_MESSAGE, _config[Constants.CONFIG_ZAPIER_MESSAGEURL]).ConfigureAwait(false);
            await _zapierClient.AddUrl(Constants.ZAPIER_EVENTTYPE_STREAMSUMMARY, _config[Constants.CONFIG_ZAPIER_STREAMSUMMARYURL]).ConfigureAwait(false);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e) => Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Completed joining the channel {e.Channel}");

        private void Client_OnConnected(object sender, OnConnectedArgs e) => Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Connected to Twitch");

        private void Client_OnLog(object sender, OnLogArgs e) => Console.WriteLine($"{e.DateTime.ToUniversalTime().ToString(CultureInfo.InvariantCulture)}: {e.BotUsername} - {e.Data}");

        private async Task Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            var date = DateTime.UtcNow;
            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Completed leaving the channel {e.Channel}");
            await _calculator.CalculateStreamStatistics(e.Channel, date).ConfigureAwait(false);

        }

        private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e) => Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Disconnected from Twitch");

        private async Task Client_OnExisingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            var date = DateTime.UtcNow;
            Console.WriteLine($"{date.ToRowKeyString()}: Existing users detected in {e.Channel}: {string.Join(", ", e.Users)}");
            foreach (var user in e.Users)
            {
                var entity = new ChannelActivityEntity
                {
                    PartitionKey = user,
                    RowKey = date.ToRowKeyString(),
                    Activity = StreamActivity.UserJoined.ToString(),
                    Viewer = e.Channel
                };
                await Common.AddEntityToStorage(_tableClient,entity, _streamingTable).ConfigureAwait(false);
                await _zapierClient.AddChannelEvent(Constants.ZAPIER_EVENTTYPE_MESSAGE, entity).ConfigureAwait(false) ;
            }
        }

        private async Task Client_OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            var date = DateTime.UtcNow;
            Console.WriteLine($"{date.ToRowKeyString()}: {e.Username} joined the channel ({e.Channel})");
            var entity = new ChannelActivityEntity
            {
                PartitionKey = e.Channel,
                RowKey = date.ToRowKeyString(),
                Activity = StreamActivity.UserJoined.ToString(),
                Viewer = e.Username
            };
            await Common.AddEntityToStorage(_tableClient, entity, _streamingTable).ConfigureAwait(false);
            await _zapierClient.AddChannelEvent(Constants.ZAPIER_EVENTTYPE_MESSAGE, entity).ConfigureAwait(false);

        }

        private async Task Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var date = DateTime.UtcNow;
            Console.WriteLine($"{date.ToRowKeyString()}: Message Posted");
            var entity = new ChannelActivityEntity
            {
                PartitionKey = e.ChatMessage.Channel,
                RowKey = date.ToRowKeyString(),
                Activity = StreamActivity.MessagePosted.ToString(),
                Viewer = e.ChatMessage.Username
            };
            await Common.AddEntityToStorage(_tableClient, entity, _streamingTable).ConfigureAwait(false);
            await _zapierClient.AddChannelEvent(Constants.ZAPIER_EVENTTYPE_MESSAGE, entity).ConfigureAwait(false);

        }
        private async Task Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            var date = DateTime.UtcNow;
            Console.WriteLine($"{date.ToRowKeyString()}: New Subscriber Posted");
            var entity = new ChannelActivityEntity
            {
                PartitionKey = e.Channel,
                RowKey = date.ToRowKeyString(),
                Activity = StreamActivity.UserSubscribed.ToString(),
                Viewer = e.Subscriber.Id
            };
            await Common.AddEntityToStorage(_tableClient, entity, _streamingTable).ConfigureAwait(false);
            await _zapierClient.AddChannelEvent(Constants.ZAPIER_EVENTTYPE_MESSAGE, entity).ConfigureAwait(false);

        }

        private async Task Client_OnUserLeft(object sender, OnUserLeftArgs e)
        {
            var date = DateTime.UtcNow;
            Console.WriteLine($"{date.ToRowKeyString()}: {e.Username} left the channel {e.Channel}");
            var entity = new ChannelActivityEntity
            {
                PartitionKey = e.Channel,
                RowKey = date.ToRowKeyString(),
                Activity = StreamActivity.UserLeft.ToString(),
                Viewer = e.Username
            };

            await Common.AddEntityToStorage(_tableClient, entity, _streamingTable).ConfigureAwait(false);
            await _zapierClient.AddChannelEvent(Constants.ZAPIER_EVENTTYPE_MESSAGE, entity).ConfigureAwait(false);

        }

        private async Task Start(string channel)
        {
            // Join the Twitch Channel
            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Joining the channel {channel}");
            if (!_twitchClient.JoinedChannels.Any(x =>
                string.Equals(channel, x.Channel, StringComparison.InvariantCultureIgnoreCase) && _twitchClient.IsConnected))
            {
                _twitchClient.JoinChannel(channel);
            }
            // Start the timer
            await _calculator.StartViewerTimer(channel);


            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Joined the channel {channel}");
            await Task.CompletedTask;
        }

        private async Task Stop(string channel)
        {
            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Stopping the channel {channel}");
            if (_twitchClient.JoinedChannels.Any(x =>
                string.Equals(x.Channel, channel, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Leaving the channel {channel}");
                _twitchClient.LeaveChannel(channel);
                Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Left the channel {channel}");
            }

            Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Stopped the channel {channel}");

            await _calculator.StopViewerTimer(channel);
        }

   

        
    }
}
