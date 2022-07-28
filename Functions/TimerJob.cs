using System;
using System.Text.Json;
using Azure.Data.Tables;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class TimerJob
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
    public async Task<QueueResponse<EventModel>> Run(
        [TimerTrigger(Constants.TIMER_SCHEDULE)]
        MyInfo myTimer)
    {
        // _logger.LogInformation($"🔨 _jsonSerializerOptions: {_jsonSerializerOptions == null}");
        _logger.LogInformation($"⚡️ Timer trigger function executed at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        // _logger.LogInformation($"🔨 Next timer schedule at: {myTimer.ScheduleStatus?.Next}");

        var result = new QueueResponse<EventModel>();

        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var tableClient = new TableClient(storageAccountConnectionString, Constants.TABLE_NAME);

        await tableClient.CreateIfNotExistsAsync();

        var time = DateTime.UtcNow
            .AddMinutes(Constants.DELAYED_MIN * -1)
            .ToString("yyyy-MM-ddTHH:mm:ssZ");

        var filter = $"{nameof(EventTableModel.Created)} lt datetime'{time}'";

        // _logger.LogInformation($"🔨 filter: {filter}");

        var items = tableClient.QueryAsync<EventTableModel>(filter: filter).AsPages();

        await foreach (var item in items)
        {
            _logger.LogInformation($">> Filtered items count: {item.Values.Count}");

            foreach (var entry in item.Values)
            {
                var model = JsonSerializer.Deserialize<EventModel>(entry.Json, _jsonSerializerOptions);
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
