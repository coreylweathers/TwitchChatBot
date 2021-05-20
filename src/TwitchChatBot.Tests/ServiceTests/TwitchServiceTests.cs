using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchChatBot.Client.Services;
using Xunit;
using Moq;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Tests.FakeDoubles;
using TwitchLib.Client;

namespace TwitchChatBot.Tests.ServiceTests
{
    public class TwitchServiceTests
    {
        private ITwitchService _sut;
        private Mock<IOptionsMonitor<TwitchOptions>> _twitchOptionsMonitorMock;
        private Mock<IOptionsMonitor<TableStorageOptions>> _tableStorageoptionsMonitorMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IStorageService> _storageServiceMock;
        private Mock<ILogger<TwitchService>> _loggerMock;
        private ITwitchHttpClient _fakeTwitchHttpClient;

        public TwitchServiceTests()
        {
            _twitchOptionsMonitorMock = new Mock<IOptionsMonitor<TwitchOptions>>();
            _tableStorageoptionsMonitorMock = new Mock<IOptionsMonitor<TableStorageOptions>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _storageServiceMock = new Mock<IStorageService>();
            _loggerMock = new Mock<ILogger<TwitchService>>();
            _fakeTwitchHttpClient = new FakeTwitchHttpClient();
        }
        
        // [Fact(Display = "methodname some condition expected result")]
        [Fact(DisplayName = "Get Banned List returns Distinct List of Banned Users for Valid Channels")]
        public void GetBannedList_WithValidChannelList_ShouldReturnDistinctBannedUserList()
        {
            // ARRANGE
            _storageServiceMock
                .Setup(x => x.GetTwitchChannels())
                .Returns(Task.FromResult(new List<string> {"testChannel"}));
            _sut = new TwitchService(_twitchOptionsMonitorMock.Object, _tableStorageoptionsMonitorMock.Object, _httpContextAccessorMock.Object, _fakeTwitchHttpClient, _storageServiceMock.Object, _loggerMock.Object);

            // ACT
            var result = _sut.GetBannedList(null);

            // ASSERT
            Assert.NotEmpty(result.Result);
        }
    }
}