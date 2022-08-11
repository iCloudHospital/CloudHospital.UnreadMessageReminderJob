using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class CallingReminderTimerJob : FunctionBase
{
    public CallingReminderTimerJob(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
        : base(debugConfigurationAccessor)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<CallingReminderTimerJob>();
    }

    [Function(Constants.CALLING_REMINDER_TIMER_TRIGGER)]
    public async Task<CallingReminderQueueResponse> Run(
        [TimerTrigger(Constants.CALLING_REMINDER_TIMER_SCHEDULE)]
        MyInfo myTimer)
    {
        _logger.LogInformation($"⚡️ {nameof(CallingReminderTimerJob)} Timer trigger function executed at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        CallingReminderQueueResponse response = new();

        var callingReminderBasis = Environment.GetEnvironmentVariable(Constants.ENV_CALLING_REMINDER_BASIS);

        if (!int.TryParse(callingReminderBasis, out int callingReminderBasisValue))
        {
            callingReminderBasisValue = 30;
        }

        // Make sure to exists table in table storage
        var tableName = GetTableNameForCallingReminder();
        var tableClient = await GetTableClient(tableName);
        // Make sure exists queue in queue storage
        var queueName = GetQueueNameForCallingReminder();
        var queueClient = await GetQueueClient(queueName);

        if (IsInDebug)
        {
            _logger.LogInformation(@$"🔨 {nameof(CallingReminderTimerJob)} Timer trigger information:
Timer schedule           : {Environment.GetEnvironmentVariable(Constants.ENV_CALLING_REMINDER_TIMER_SCHEDULE)}        
Table                    : {(tableClient.Name == GetTableNameForCallingReminder() ? "✅ Ready" : "❌ Table is not READY")}
Queue                    : {(queueClient.Name == GetQueueNameForCallingReminder() ? "✅ Ready" : "❌ Queue is not READY")}
Calling reminder minutes : {callingReminderBasisValue} MIN
");
        }

        var from = DateTime.UtcNow.AddMinutes(callingReminderBasisValue * -1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var to = DateTime.UtcNow.AddMinutes(callingReminderBasisValue).ToString("yyyy-MM-ddTHH:mm:ssZ");

        var filter = $"{nameof(ConsultationTableModel.ConfirmedDateStart)} ge datetime'{from}' and {nameof(ConsultationTableModel.ConfirmedDateStart)} le datetime'{to}'";

        if (IsInDebug)
        {
            _logger.LogInformation("🔨 filter: {filter}", filter);
        }

        var queryResult = tableClient.QueryAsync<ConsultationTableModel>(
            // x => from <= x.ConfirmedDateStart && to >= x.ConfirmedDateStart
            filter: filter
            ).AsPages();

        await foreach (var result in queryResult)
        {
            foreach (var item in result.Values)
            {
                if (!string.IsNullOrWhiteSpace(item.Json))
                {
                    var consultation = JsonSerializer.Deserialize<ConsultationModel>(item.Json, _jsonSerializerOptions);
                    if (consultation != null)
                    {
                        // enqueue
                        response.Items.Add(consultation);

                        if (IsInDebug)
                        {
                            _logger.LogInformation($"Enqueue item: consultation.Id={consultation.Id}");
                        }

                        // Remove item in table 
                        await tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
                        if (IsInDebug)
                        {
                            _logger.LogInformation($">> Remove item from table: PartitionKey={item.PartitionKey}, RowKey={item.RowKey}");
                        }
                    }
                    else
                    {
                        if (IsInDebug)
                        {
                            _logger.LogWarning($"Deserialization was faild. partitionkey={item.PartitionKey}, rowkey={item.RowKey}");
                        }
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

    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ILogger _logger;
}

/// <summary>
/// Queue output binding wrapper
/// </summary>
public class CallingReminderQueueResponse
{
    [QueueOutput(Constants.CALLING_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
    public List<ConsultationModel> Items { get; set; } = new();
}