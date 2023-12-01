using System.Net;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class CallingReminderConsultationUpdatedWebhook : HttpTriggerFunctionBase
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CallingReminderConsultationUpdatedWebhook(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
        : base(debugConfigurationAccessor)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<CallingReminderConsultationUpdatedWebhook>();
    }

    [Function(Constants.CALLING_REMINDER_HTTP_TRIGGER)]
    public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
    {
        _logger.LogInformation($"⚡️ [{nameof(CallingReminderConsultationUpdatedWebhook)}] HTTP trigger function processed a request.");

        var response = CreateResponse(req, HttpStatusCode.OK);

        var payload = string.Empty;

        if (req.Body == null || req.Body.Length == 0)
        {
            _logger.LogWarning("Payload is empty.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        byte[] payloadBinary = null;
        using (var memoryStream = new MemoryStream())
        {
            req.Body.Position = 0;
            await req.Body.CopyToAsync(memoryStream);
            await req.Body.FlushAsync();

            payloadBinary = memoryStream.ToArray();

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);

            payload = await reader.ReadToEndAsync();
            reader.Close();
        }

        if (IsInDebug)
        {
            _logger.LogInformation($"Payload: {payload}");
        }

        if (string.IsNullOrWhiteSpace(payload) || payloadBinary == null || payloadBinary.Length == 0)
        {
            _logger.LogWarning("Payload is empty.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        var consultation = JsonSerializer.Deserialize<Models.ConsultationModel>(payload, _jsonSerializerOptions);

        if (consultation == null)
        {
            _logger.LogWarning("Fail to parse Payload to Object.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        if (IsInDebug)
        {
            _logger.LogInformation("consultation: Id={id},IsOpen={isOpen}", consultation.Id, consultation.IsOpen);
        }

        var tableName = GetTableNameForCallingReminder();
        var tableClient = await GetTableClient(tableName);

        var queueName = GetQueueNameForCallingReminder();
        var queueClient = await GetQueueClient(queueName);

        if (IsInDebug)
        {
            var callingReminderBasis = Environment.GetEnvironmentVariable(Constants.ENV_CALLING_REMINDER_BASIS);
            if (!int.TryParse(callingReminderBasis, out int callingReminderBasisValue))
            {
                callingReminderBasisValue = 30;
            }

            _logger.LogInformation(@$"🔨 {nameof(CallingReminderConsultationUpdatedWebhook)} information:
Timer schedule         : {Environment.GetEnvironmentVariable(Constants.ENV_CALLING_REMINDER_TIMER_SCHEDULE)}        
Table                  : {(tableClient.Name == GetTableNameForCallingReminder() ? "✅ Ready" : "❌ Table is not READY")}
Queue                  : {(queueClient.Name == GetQueueNameForCallingReminder() ? "✅ Ready" : "❌ Queue is not READY")}
Unread delayed minutes : {callingReminderBasisValue} MIN
        ");
        }

        var filter = $"{nameof(ConsultationTableModel.PartitionKey)} eq '{consultation.Id}'";
        var queryResult = tableClient.QueryAsync<ConsultationTableModel>(filter: filter).AsPages();

        // If consultation is exists, remove all consultations
        await foreach (var result in queryResult)
        {
            foreach (var item in result.Values)
            {
                await tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
                if (IsInDebug)
                {
                    _logger.LogInformation($"Remove old data. {nameof(ConsultationTableModel.PartitionKey)}={item.PartitionKey}");
                }
            }
        }

        if (consultation.IsOpen)
        {
            // If consultation is opened state, Add entry.
            var entry = new ConsultationTableModel
            {
                PartitionKey = consultation.Id,
                RowKey = Guid.NewGuid().ToString(),
                ConfirmedDateStart = consultation.ConfirmedDateStart,
                Json = payload,
            };

            await tableClient.AddEntityAsync(entry);

            if (IsInDebug)
            {
                _logger.LogInformation($"Add consultation data to Table. {nameof(ConsultationTableModel.PartitionKey)}={consultation.Id}");
            }
        }

        return response;
    }

}

