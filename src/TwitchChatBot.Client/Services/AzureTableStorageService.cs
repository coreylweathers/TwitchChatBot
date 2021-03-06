using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchChatBot.Client.Extensions;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Shared.Models.Entities;

namespace TwitchChatBot.Client.Services
{
    public class AzureTableStorageService : IStorageService
    {
        private readonly TableStorageOptions _tableStorageOptions;
        private readonly ILogger<AzureTableStorageService> _logger;
        private readonly CloudTableClient _tableClient;

        public AzureTableStorageService(IOptionsMonitor<TableStorageOptions> optionsMonitor, ILogger<AzureTableStorageService> logger)
        {
            _tableStorageOptions = optionsMonitor.CurrentValue;
            _logger = logger;
            _tableClient = CreateTableClient(_tableStorageOptions.ConnectionString);
        }

        public async Task<TableEntity> AddDataToStorage(TableEntity entity, string tableName)
        {
            var insertOperation = TableOperation.InsertOrMerge(entity);
            var table = _tableClient.GetTableReference(tableName);
            if (!await table.ExistsAsync())
            {
                await table.CreateAsync();
            }

            var result = await table.ExecuteAsync(insertOperation);
            return result.Result as TableEntity;
        }

        public async Task<SubscriptionActivityEntity> GetSubscriptionStatus(string partitionKey)
        {
            var table = _tableClient.GetTableReference(_tableStorageOptions.SubscriptionTable);
            
            var result = (from entity in table.CreateQuery<SubscriptionActivityEntity>()
                         where entity.PartitionKey == partitionKey
                         orderby entity.Timestamp descending
                         select entity).Take(1).FirstOrDefault();

            return await Task.FromResult(result);
        }

        public async Task<List<string>> GetTwitchChannels()
        {
            List<string> results = null;
            var table = _tableClient.ListTables().FirstOrDefault(table => string.Equals(table.Name, _tableStorageOptions.SettingsTable,StringComparison.CurrentCultureIgnoreCase));
            
            if (table == null)
            {
                await table.CreateAsync();
                // TODO: If the table is just created, add the data to the table (for local debug purposes)
            }
            else 
            {
            // TODO: Wrap this in a todo so we can get the error that occurs when this can't connect to a table
             results = (from entity in table.CreateQuery<SettingsEntity>()
                          select entity.RowKey).ToList().Distinct().ToList();
            }
            return await Task.FromResult(results);
        }
         
        private CloudTableClient CreateTableClient(string connectionString)
        {
            _logger.LogFormattedMessage("Creating Table Client");
            try
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                return storageAccount.CreateCloudTableClient();
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception has occurred", ex);
            }
            return null;
        }
    }
}
