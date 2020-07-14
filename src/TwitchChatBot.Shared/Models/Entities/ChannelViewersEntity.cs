using Microsoft.Azure.Cosmos.Table;
using System;

namespace TwitchChatBot.Shared.Models.Entities
{
    // https://blog.bitscry.com/2019/04/12/adding-complex-properties-of-a-tableentity-to-azure-table-storage/
    public class ChannelViewersEntity : TableEntity
    {
        // Partition Key = Channel Name
        // Row Key = Current Timestamp
        public DateTime StartedAt { get; set; }
        public int ViewerCount { get; set; }
    }
}
