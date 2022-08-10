using System;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class UnreadMessageReminderTimerJob : FunctionBase
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private readonly ILogger _logger;

    public UnreadMessageReminderTimerJob(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
        : base()
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<UnreadMessageReminderTimerJob>();
    }

    [Function(Constants.UNREAD_MESSAGE_REMINDER_TIMER_TRIGGER)]
    public async Task<UnreadMessageReminderQueueResponse> Run(
        [TimerTrigger(Constants.UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE)]
        MyInfo myTimer)
    {
        _logger.LogInformation($"⚡️ {nameof(UnreadMessageReminderTimerJob)} Timer trigger function executed at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");

        var unreadDelayMinutes = Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_DELAY_MINUTES);
        // int unreadDelayMinutesValue = 0;
        if (!int.TryParse(unreadDelayMinutes, out int unreadDelayMinutesValue))
        {
            unreadDelayMinutesValue = 5;
        }

        var response = new UnreadMessageReminderQueueResponse();

        // Make sure to exists table in table storage
        var tableName = GetTableNameForUnreadMessageReminder();
        var tableClient = await GetTableClient(tableName);
        // Make sure exists queue in queue storage
        var queueName = GetQueueNameForUnreadMessageRemider();
        var queueClient = await GetQueueClient(queueName);

        if (IsInDebug)
        {
            _logger.LogInformation(@$"🔨 {nameof(UnreadMessageReminderTimerJob)} Timer trigger information:
Timer schedule         : {Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE)}        
Table                  : {(tableClient.Name == GetTableNameForUnreadMessageReminder() ? "✅ Ready" : "❌ Table is not READY")}
Queue                  : {(queueClient.Name == GetQueueNameForUnreadMessageRemider() ? "✅ Ready" : "❌ Queue is not READY")}
Unread delayed minutes : {unreadDelayMinutesValue} MIN
");
        }

        var time = DateTime.UtcNow
            .AddMinutes(unreadDelayMinutesValue * -1)
            .ToString("yyyy-MM-ddTHH:mm:ssZ");

        var filter = $"{nameof(EventTableModel.Created)} lt datetime'{time}'";

        if (IsInDebug)
        {
            _logger.LogInformation($"🔨 filter: {filter}");
        }

        var queryResults = tableClient.QueryAsync<EventTableModel>(filter: filter).AsPages();

        await foreach (var result in queryResults)
        {
            if (IsInDebug)
            {
                _logger.LogInformation($">> Filtered items count: {result.Values.Count}");
            }

            foreach (var item in result.Values)
            {
                var model = JsonSerializer.Deserialize<SendBirdGroupChannelMessageSendEventModel>(item.Json, _jsonSerializerOptions);
                if (model != null)
                {
                    // enqueue
                    response.Items.Add(model);
                    if (IsInDebug)
                    {
                        _logger.LogInformation($">> Enqueue item.");
                    }

                    // Remove item in table 
                    await tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
                    if (IsInDebug)
                    {
                        _logger.LogInformation($">> Remove item from table. PartitionKey={item.PartitionKey}, RowKey={item.RowKey}");
                    }
                }
                else
                {
                    if (IsInDebug)
                    {
                        _logger.LogWarning($"Deserialization was faild. partitionkey: {item.PartitionKey}, rowkey:{item.RowKey}");
                    }
                }
            }
        }

        if (IsInDebug)
        {
            _logger.LogInformation($"🔨 {response.Items.Count} items enqueued.");
        }

        return response;
    }
}

/// <summary>
/// Queue output binding wrapper
/// </summary>
public class UnreadMessageReminderQueueResponse
{
    [QueueOutput(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
    public List<SendBirdGroupChannelMessageSendEventModel> Items { get; set; } = new();
}

public class MyInfo
{
    public MyScheduleStatus ScheduleStatus { get; set; }

    public bool IsPastDue { get; set; }
}

public class MyScheduleStatus
{
    public DateTime Last { get; set; }

    public DateTime Next { get; set; }

    public DateTime LastUpdated { get; set; }
}
