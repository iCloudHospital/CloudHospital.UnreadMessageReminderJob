using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

/// <summary>
/// sendbird webhook: group_channel:message_send 
/// </summary>
public class GroupChannelMessageSendWebHook : HttpTriggerFunctionBase
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    public GroupChannelMessageSendWebHook(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory) : base()
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<GroupChannelMessageSendWebHook>();
    }

    [Function("GroupChannelMessageSendWebHook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req)
    {
        _logger.LogInformation($"⚡️ [{nameof(GroupChannelMessageSendWebHook)}] HTTP trigger function processed a request.");

        SendBirdGroupChannelMessageSendEventModel model = null;

        var response = CreateResponse(req, HttpStatusCode.OK);

        var payload = string.Empty;

        using (var reader = new StreamReader(req.Body))
        {
            payload = await reader.ReadToEndAsync();
            reader.Close();
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("Payload is empty.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        try
        {
            model = JsonSerializer.Deserialize<SendBirdGroupChannelMessageSendEventModel>(payload, _jsonSerializerOptions);
        }
        catch
        {
            _logger.LogWarning("Payload could not deserialize.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }


        if (model == null)
        {
            _logger.LogWarning("Payload could not deserialize.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        if (IsInDebug)
        {
            _logger.LogInformation($"Payload #1: {payload}");
            // _logger.LogInformation($"Payload #2: {JsonSerializer.Serialize(model, _jsonSerializerOptions)}");
            // _logger.LogInformation($"GroupId: {model.GroupId}");
        }

        var entry = new EventTableModel
        {
            PartitionKey = model.Channel.ChannelUrl,
            RowKey = Guid.NewGuid().ToString(),
            Message = "Hello world from Table!!",
            Json = payload,
            Created = DateTime.UtcNow, //model.Payload.CreatedAt,
        };

        // SendBirdGroupChannelMessageSendEventModel.Sender 에 따라 처리할 내용이 다릅니다.
        // TODO: 사용자 타입 데이터를 어떤 필드에서 확인할 수 있는지 확인 필요
        // When sender: one of [ChManager, Manager]

        var tableClient = await GetTableClient();

        var queryResult = tableClient.QueryAsync<EventTableModel>(filter: $"{nameof(EventTableModel.PartitionKey)} eq '{model.Channel.ChannelUrl}'").AsPages();

        await foreach (var result in queryResult)
        {
            foreach (var item in result.Values)
            {
                await tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
                if (IsInDebug)
                {
                    _logger.LogInformation($"Remove old data. {nameof(EventTableModel.PartitionKey)}={model.Channel.ChannelUrl}");
                }
            }
        }

        await tableClient.AddEntityAsync(entry);
        if (IsInDebug)
        {
            _logger.LogInformation($"Add event data to Table. {nameof(EventTableModel.PartitionKey)}={model.Channel.ChannelUrl}");
        }

        return response;
    }
}
