using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    public class BotWorker : BackgroundService
    {
        private readonly ILogger<BotWorker> _logger;
        private readonly IConfiguration _config;
        private ZapierClient _zapierClient;
        private TwitchClient _twitchClient;
        private CloudTableClient _tableClient;
        private readonly string _streamingTable;
        private readonly Calculator _calculator;

        public BotWorker(ILogger<BotWorker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _streamingTable = _config[Constants.CONFIG_TABLE_STREAMINGTABLE];
            _calculator = new Calculator(_config);
        }

        private async Task Initialize()
        {
            await SetupTwitchClient().ConfigureAwait(false);
            _tableClient = await Common.CreateTableClient(_config[Constants.CONFIG_TABLE_CONNECTIONSTRING])
                .ConfigureAwait(false);
            await SetupSignalR().ConfigureAwait(false);
            await SetupZapierClient().ConfigureAwait(false);
        }

        private async Task SetupSignalR()
        {
            var uri = new Uri($"{_config[Constants.CONFIG_SIGNALR_URI]}");
            var hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(uri)
                .Build();

            hubConnection.On<ChannelActivityEntity>("UpdateChannelState", async entity =>
            {
                var activity = (StreamActivity) Enum.Parse(typeof(StreamActivity), entity.Activity);
                switch (activity)
                {
                    case StreamActivity.StreamStarted:
                        await Start(entity.PartitionKey);
                        break;
                    case StreamActivity.StreamStopped:
                        await Stop(entity.PartitionKey);
                        break;
                    case StreamActivity.MessagePosted:
                        break;
                    case StreamActivity.UserJoined:
                        break;
                    case StreamActivity.UserLeft:
                        break;
                    case StreamActivity.UserFollowed:
                        break;
                    case StreamActivity.UserSubscribed:
                        break;
                    case StreamActivity.ViewerTimestamped:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            hubConnection.On<string>("UpdatePassword", async passwordText => { await UpdatePassword(passwordText); });
            await hubConnection.StartAsync().ConfigureAwait(false);
            _logger.LogInformation($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Connected to SignalR hub {uri} - {hubConnection.ConnectionId}");
        }

        private async Task UpdatePassword(string password)
        {
            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Updating the Twitch bot password");
            var creds = new ConnectionCredentials(_config[Constants.CONFIG_TWITCH_USERNAME], password);

            try
            {
                // IF CONNECTED, DISCONNECT
                if (_twitchClient.IsConnected)
                {
                    _logger.LogInformation($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Disconnecting from Twitch");
                    _twitchClient.Disconnect();
                    _logger.LogInformation($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Disconnected from Twitch");
                }

                // UPDATE THE CLIENT CREDS
                _twitchClient.SetConnectionCredentials(creds);

                // RECONNECT TO TWITCH
                _twitchClient.Reconnect();
                _logger.LogInformation($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Updated the Twitch bot password");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: An exception occured: {ex.Message}");
            }

            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Updated the Twitch bot password");
            await Task.CompletedTask;
        }

        private async Task SetupTwitchClient()
        {
            var credentials =
                new ConnectionCredentials(_config[Constants.CONFIG_TWITCH_USERNAME],
                    _config[Constants.CONFIG_TWITCH_PASSWORD]);
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
            _twitchClient.OnLeftChannel += async (s, e) => await Client_OnLeftChannel(s, e);
            _twitchClient.OnMessageReceived += async (s, e) => await Client_OnMessageReceived(s, e);
            _twitchClient.OnNewSubscriber += async (s, e) => await Client_OnNewSubscriber(s, e);
            _twitchClient.OnUserJoined += async (s, e) => await Client_OnUserJoined(s, e);
            _twitchClient.OnUserLeft += async (s, e) => await Client_OnUserLeft(s, e);
            _twitchClient.OnExistingUsersDetected += async (s, e) => await Client_OnExistingUsersDetected(s, e);

            _twitchClient.Connect();
            
            // TODO: Remove this as this is only for debug purposes
            _twitchClient.JoinChannel("cldubya");

            await Task.CompletedTask;
        }

        private async Task SetupZapierClient()
        {
            _zapierClient = new ZapierClient();
            await _zapierClient.AddUrl(Constants.ZAPIER_EVENTTYPE_MESSAGE, _config[Constants.CONFIG_ZAPIER_MESSAGEURL])
                .ConfigureAwait(false);
            await _zapierClient
                .AddUrl(Constants.ZAPIER_EVENTTYPE_STREAMSUMMARY, _config[Constants.CONFIG_ZAPIER_STREAMSUMMARYURL])
                .ConfigureAwait(false);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e) => _logger.LogInformation(
            $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Completed joining the channel {e.Channel}");

        private void Client_OnConnected(object sender, OnConnectedArgs e) =>
            _logger.LogInformation($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Connected to Twitch");

        private void Client_OnLog(object sender, OnLogArgs e) => _logger.LogInformation(
            $"{e.DateTime.ToUniversalTime().ToString(CultureInfo.CurrentUICulture)}: {e.BotUsername} - {e.Data}");

        private async Task Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            var date = DateTime.UtcNow;
            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Completed leaving the channel {e.Channel}");
            await _calculator.CalculateStreamStatistics(e.Channel, date).ConfigureAwait(false);
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e) =>
            _logger.LogInformation($"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Disconnected from Twitch");

        private async Task Client_OnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            var date = DateTime.UtcNow;
            _logger.LogInformation(
                $"{date.ToRowKeyString()}: Existing users detected in {e.Channel}: {string.Join(", ", e.Users)}");
            foreach (var entity in e.Users.Select(user => new ChannelActivityEntity
            {
                PartitionKey = user,
                RowKey = date.ToRowKeyString(),
                Activity = StreamActivity.UserJoined.ToString(),
                Viewer = e.Channel
            }))
            {
                await Common.AddEntityToStorage(_tableClient, entity, _streamingTable).ConfigureAwait(false);
                await _zapierClient.AddChannelEvent(Constants.ZAPIER_EVENTTYPE_MESSAGE, entity).ConfigureAwait(false);
            }
        }

        private async Task Client_OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            var date = DateTime.UtcNow;
            _logger.LogInformation($"{date.ToRowKeyString()}: {e.Username} joined the channel ({e.Channel})");
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
            _logger.LogInformation($"{date.ToRowKeyString()}: Message Posted");
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
            _logger.LogInformation($"{date.ToRowKeyString()}: New Subscriber Posted");
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
            _logger.LogInformation($"{date.ToRowKeyString()}: {e.Username} left the channel {e.Channel}");
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
            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Joining the channel {channel}");
            if (!_twitchClient.JoinedChannels.Any(x =>
                string.Equals(channel, x.Channel, StringComparison.InvariantCultureIgnoreCase) &&
                _twitchClient.IsConnected))
            {
                _twitchClient.JoinChannel(channel);
            }

            // Start the timer
            await _calculator.StartViewerTimer(channel);

            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Joined the channel {channel}");
            await Task.CompletedTask;
        }

        private async Task Stop(string channel)
        {
            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Stopping the channel {channel}");
            if (_twitchClient.JoinedChannels.Any(x =>
                string.Equals(x.Channel, channel, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogInformation(
                    $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Leaving the channel {channel}");
                _twitchClient.LeaveChannel(channel);
                _logger.LogInformation(
                    $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Left the channel {channel}");
            }
            _logger.LogInformation(
                $"{DateTime.UtcNow.ToString(CultureInfo.CurrentUICulture)}: Stopped the channel {channel}");

            await _calculator.StopViewerTimer(channel);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Initialize();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}