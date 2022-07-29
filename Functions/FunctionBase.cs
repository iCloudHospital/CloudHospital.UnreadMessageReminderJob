using Azure.Data.Tables;
using Azure.Storage.Queues;

namespace CloudHospital.UnreadMessageReminderJob;

public abstract class FunctionBase
{
    protected async Task<TableClient> GetTableClient()
    {
        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var tableName = Environment.GetEnvironmentVariable(Constants.ENV_TABLE_NAME);

        var tableClient = new TableClient(storageAccountConnectionString, tableName);

        await tableClient.CreateIfNotExistsAsync();

        return tableClient;
    }

    protected async Task<QueueClient> GetQueueClient()
    {
        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var queueName = Environment.GetEnvironmentVariable(Constants.ENV_QUEUE_NAME);

        var queueClient = new QueueClient(storageAccountConnectionString, queueName);
        await queueClient.CreateIfNotExistsAsync();

        return queueClient;
    }
}