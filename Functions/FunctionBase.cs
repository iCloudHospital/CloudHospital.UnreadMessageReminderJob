using Azure.Data.Tables;
using Azure.Storage.Queues;

namespace CloudHospital.UnreadMessageReminderJob;

public abstract class FunctionBase
{
    public FunctionBase()
    {
        isInDebug = GetDebugMode();
    }

    protected bool IsInDebug => isInDebug;

    protected async Task<TableClient> GetTableClient(string tableName)
    {
        // var tableName = GetTableName();

        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var tableClient = new TableClient(storageAccountConnectionString, tableName);

        await tableClient.CreateIfNotExistsAsync();

        return tableClient;
    }

    protected string GetTableNameForUnreadMessageReminder()
    {
        var stage = Environment.GetEnvironmentVariable(Constants.ENV_STAGE);
        var tableName = Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_MESSAGE_REMINDER_TABLE_NAME);

        var tableNameActural = $"{tableName}{stage}";

        return tableNameActural;
    }

    protected string GetTableNameForCallingReminder()
    {
        var stage = Environment.GetEnvironmentVariable(Constants.ENV_STAGE);
        var tableName = Environment.GetEnvironmentVariable(Constants.ENV_CALLING_REMINDER_TABLE_NAME);

        var tableNameActural = $"{tableName}{stage}";

        return tableNameActural;
    }

    protected async Task<QueueClient> GetQueueClient(string queueName)
    {
        // var queueName = GetQueueName();

        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var queueClient = new QueueClient(storageAccountConnectionString, queueName);
        await queueClient.CreateIfNotExistsAsync();

        return queueClient;
    }

    protected string GetQueueNameForUnreadMessageRemider()
    {
        var stage = Environment.GetEnvironmentVariable(Constants.ENV_STAGE);
        var queueName = Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_MESSAGE_REMINDER_QUEUE_NAME);
        var queueNameActural = $"{queueName}{stage}";

        return queueNameActural;
    }

    protected string GetQueueNameForCallingReminder()
    {
        var stage = Environment.GetEnvironmentVariable(Constants.ENV_STAGE);
        var queueName = Environment.GetEnvironmentVariable(Constants.ENV_CALLING_REMINDER_QUEUE_NAME);
        var queueNameActural = $"{queueName}{stage}";

        return queueNameActural;
    }

    private bool GetDebugMode()
    {
        var debug = Environment.GetEnvironmentVariable(Constants.ENV_DEBUG);
        if (!bool.TryParse(debug, out bool isDebug))
        {
            return false;
        }

        return isDebug;
    }



    private readonly bool isInDebug;
}