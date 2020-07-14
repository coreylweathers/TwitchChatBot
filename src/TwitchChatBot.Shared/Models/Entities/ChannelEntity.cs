using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;

namespace TwitchChatBot.Shared.Models.Entities
{
    public class ChannelEntity : TableEntity
    {
        // PARTITION KEY = Settings Enum Value
        // ROW KEY = TimeStamp 

        [EntityPropertyConverter]
        public List<string> Channels { get; set; }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return base.WriteEntity(operationContext);
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
        }
    }
}
