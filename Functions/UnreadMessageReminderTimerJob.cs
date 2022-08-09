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
    public async Task<QueueResponse<SendBirdGroupChannelMessageSendEventModel>> Run(
        [TimerTrigger(Constants.UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE)]
        MyInfo myTimer)
    {
        _logger.LogInformation($"⚡️ Timer trigger function executed at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        if (IsInDebug)
        {
            // _logger.LogInformation($"🔨 _jsonSerializerOptions: {_jsonSerializerOptions == null}");
            // _logger.LogInformation($"🔨 Next timer schedule at: {myTimer.ScheduleStatus?.Next}");
        }

        var unreadDelayMinutes = Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_DELAY_MINUTES);
        // int unreadDelayMinutesValue = 0;
        if (!int.TryParse(unreadDelayMinutes, out int unreadDelayMinutesValue))
        {
            unreadDelayMinutesValue = 5;
        }

        var result = new QueueResponse<SendBirdGroupChannelMessageSendEventModel>();

        // Make sure to exists table in table storage
        var tableName = GetTableNameForUnreadMessageReminder();
        var tableClient = await GetTableClient(tableName);
        // Make sure exists queue in queue storage
        var queueName = GetQueueNameForUnreadMessageRemider();
        var queueClient = await GetQueueClient(queueName);

        if (IsInDebug)
        {
            _logger.LogInformation(@$"🔨 Timer trigger information:
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

        var items = tableClient.QueryAsync<EventTableModel>(filter: filter).AsPages();

        await foreach (var item in items)
        {
            if (IsInDebug)
            {
                _logger.LogInformation($">> Filtered items count: {item.Values.Count}");
            }

            foreach (var entry in item.Values)
            {
                var model = JsonSerializer.Deserialize<SendBirdGroupChannelMessageSendEventModel>(entry.Json, _jsonSerializerOptions);
                if (model != null)
                {
                    // enqueue
                    result.Items.Add(model);
                    if (IsInDebug)
                    {
                        _logger.LogInformation(">> Enqueue item");
                    }

                    // Remove item in table 
                    await tableClient.DeleteEntityAsync(entry.PartitionKey, entry.RowKey);
                    if (IsInDebug)
                    {
                        _logger.LogInformation(">> Remove item from table");
                    }
                }
                else
                {
                    if (IsInDebug)
                    {
                        _logger.LogWarning($"Deserialization was faild. partitionkey: {entry.PartitionKey}, rowkey:{entry.RowKey}");
                    }
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Queue output binding wrapper
/// </summary>
/// <typeparam name="T"></typeparam>
public class QueueResponse<T>
{
    [QueueOutput(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
    public List<T> Items { get; set; } = new List<T>();
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
