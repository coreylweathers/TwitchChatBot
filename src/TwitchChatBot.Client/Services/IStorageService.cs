using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;
using TwitchChatBot.Shared.Models.Entities;

namespace TwitchChatBot.Client.Services
{
    public interface IStorageService
    {
        Task<TableEntity> AddDataToStorage(TableEntity entity, string tableName);

        Task<SubscriptionActivityEntity> GetSubscriptionStatus(string partitionKey);
    }
}
