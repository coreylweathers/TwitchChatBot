using Microsoft.Azure.Cosmos.Table;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace TwitchChatBot.CLI
{
    class Common
    {
        public static async Task<CloudTableClient> CreateTableClient(string connectionString)
        {
            CloudTableClient tableClient = null;
            try
            {
                var storage = CloudStorageAccount.Parse(connectionString);
                tableClient = storage.CreateCloudTableClient();
                await Task.CompletedTask;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"The connection string was not formatted correctly. {ex.Message}");
                await Task.FromException(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception has occurred: {ex.Message}");
                await Task.FromException(ex);
            }
            return tableClient;
        }

        public static async Task AddEntityToStorage(CloudTableClient _tableClient, TableEntity entity, string table)
        {
            try
            {
                var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
                var cloudTable = _tableClient.GetTableReference(table);
                Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Adding entity {entity} to the table {cloudTable.Name}");
                var result = await cloudTable.ExecuteAsync(insertOrMergeOperation);
                if (result.RequestCharge.HasValue)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Request Charge of InsertOrMerge Operation: {result.RequestCharge}");
                    Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: Added entity {entity} to the table {cloudTable.Name}");
                }
            }
            catch (StorageException ex)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}: An exception has occured while writing to the table", ex);
            }
        }
    }
}
