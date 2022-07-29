using System;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class TimerJob : FunctionBase
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private readonly ILogger _logger;

    public TimerJob(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<TimerJob>();
    }

    [Function("TimerJob")]
    public async Task<QueueResponse<SendBirdGroupChannelMessageSendEventModel>> Run(
        [TimerTrigger("%TimerSchedule%")]
        MyInfo myTimer)
    {
        // _logger.LogInformation($"🔨 _jsonSerializerOptions: {_jsonSerializerOptions == null}");
        _logger.LogInformation($"⚡️ Timer trigger function executed at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        // _logger.LogInformation($"🔨 Next timer schedule at: {myTimer.ScheduleStatus?.Next}");

        var unreadDelayMinutes = Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_DELAY_MINUTES);
        // int unreadDelayMinutesValue = 0;
        if (!int.TryParse(unreadDelayMinutes, out int unreadDelayMinutesValue))
        {
            unreadDelayMinutesValue = 5;
        }

        var result = new QueueResponse<SendBirdGroupChannelMessageSendEventModel>();

        // Make sure to exists table in table storage
        var tableClient = await GetTableClient();
        // Make sure exists queue in queue storage
        var queueClient = await GetQueueClient();

        _logger.LogInformation(@$"🔨 Timer trigger information:
Timer schedule         : {Environment.GetEnvironmentVariable(Constants.ENV_TIMER_SCHEDULE)}        
Table                  : {(tableClient.Name == GetTableName() ? "✅ Ready" : "❌ Table is not READY")}
Queue                  : {(queueClient.Name == GetQueueName() ? "✅ Ready" : "❌ Queue is not READY")}
Unread delayed minutes : {unreadDelayMinutesValue} MIN
        ");

        var time = DateTime.UtcNow
            .AddMinutes(unreadDelayMinutesValue * -1)
            .ToString("yyyy-MM-ddTHH:mm:ssZ");

        var filter = $"{nameof(EventTableModel.Created)} lt datetime'{time}'";

        // _logger.LogInformation($"🔨 filter: {filter}");

        var items = tableClient.QueryAsync<EventTableModel>(filter: filter).AsPages();

        await foreach (var item in items)
        {
            _logger.LogInformation($">> Filtered items count: {item.Values.Count}");

            foreach (var entry in item.Values)
            {
                var model = JsonSerializer.Deserialize<SendBirdGroupChannelMessageSendEventModel>(entry.Json, _jsonSerializerOptions);
                if (model != null)
                {
                    // enqueue
                    result.Items.Add(model);
                    _logger.LogInformation(">> Enqueue item");

                    // Remove item in table 
                    await tableClient.DeleteEntityAsync(entry.PartitionKey, entry.RowKey);
                    _logger.LogInformation(">> Remove item from table");
                }
                else
                {
                    _logger.LogWarning($"Deserialization was faild. partitionkey: {entry.PartitionKey}, rowkey:{entry.RowKey}");
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
    [QueueOutput("%QueueName%%Stage%", Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
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
