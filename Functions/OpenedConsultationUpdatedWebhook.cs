using System.Net;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class OpenedConsultationUpdatedWebhook : HttpTriggerFunctionBase
{
    public OpenedConsultationUpdatedWebhook(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<OpenedConsultationUpdatedWebhook>();
    }

    [Function(Constants.OPENED_CONSULTATION_UPDATE_HTTP_TRIGGER)]
    public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
    {
        _logger.LogInformation($"⚡️ [{nameof(OpenedConsultationUpdatedWebhook)}] HTTP trigger function processed a request.");

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
            using (var reader = new StreamReader(memoryStream))
            {
                payload = await reader.ReadToEndAsync();
                reader.Close();
            }
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

        var consultation = JsonSerializer.Deserialize<ConsultationModel>(payload);

        if (consultation == null)
        {
            _logger.LogWarning("Fail to parse Payload to Object.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        var tableName = GetTableNameForOpenedConsultationUpdate();
        var tableClient = await GetTableClient(tableName);

        var queueName = GetQueueNameForOpenedConsultationUpdate();
        var queueClient = await GetQueueClient(queueName);

        if (IsInDebug)
        {
            var openedConsultationUpdateReminderBasis = Environment.GetEnvironmentVariable(Constants.ENV_OPENED_CONSULTATION_UPDATE_REMIDER_BASIS);
            if (!int.TryParse(openedConsultationUpdateReminderBasis, out int openedConsultationUpdateReminderBasisValue))
            {
                openedConsultationUpdateReminderBasisValue = 30;
            }

            _logger.LogInformation(@$"🔨 {nameof(OpenedConsultationUpdatedWebhook)} information:
Timer schedule         : {Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE)}        
Table                  : {(tableClient.Name == GetTableNameForUnreadMessageReminder() ? "✅ Ready" : "❌ Table is not READY")}
Queue                  : {(queueClient.Name == GetQueueNameForUnreadMessageRemider() ? "✅ Ready" : "❌ Queue is not READY")}
Unread delayed minutes : {openedConsultationUpdateReminderBasisValue} MIN
        ");
        }

        var filter = $"PartitionKey eq '{consultation.Id}'";
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

    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
}

