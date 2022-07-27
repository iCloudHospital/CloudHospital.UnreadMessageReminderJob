using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class GroupChannelMessageSendWebHook
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    public GroupChannelMessageSendWebHook(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<GroupChannelMessageSendWebHook>();
    }

    [Function("GroupChannelMessageSendWebHook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
    {
        _logger.LogInformation("⚡️ HTTP trigger function processed a request.");

        // SendBirdGroupChannelMessageSendEventModel.Sender 에 따라 처리할 내용이 다릅니다.


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

        // _logger.LogInformation($"Payload #1: {payload}");
        // _logger.LogInformation($"Payload #2: {JsonSerializer.Serialize(model, _jsonSerializerOptions)}");
        // _logger.LogInformation($"GroupId: {model.GroupId}");

        var entry = new EventTableModel
        {
            PartitionKey = model.Channel.ChannelUrl,
            RowKey = Guid.NewGuid().ToString(),
            Message = "Hello world from Table!!",
            Json = payload,
            Created = model.Payload.CreatedAt,
        };

        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var tableClient = new TableClient(storageAccountConnectionString, Constants.TABLE_NAME);
        await tableClient.CreateIfNotExistsAsync();

        var queryResult = tableClient.QueryAsync<EventTableModel>(filter: $"{nameof(EventTableModel.PartitionKey)} eq '{model.Channel.ChannelUrl}'").AsPages();

        await foreach (var result in queryResult)
        {
            foreach (var item in result.Values)
            {
                await tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);
                _logger.LogInformation($"Remove old data. {nameof(EventModel.GroupId)}={model.Channel.ChannelUrl}");
            }
        }

        await tableClient.AddEntityAsync(entry);
        _logger.LogInformation($"Add event data to Table. {nameof(EventModel.GroupId)}={model.Channel.ChannelUrl}");

        await Task.Delay(TimeSpan.FromMilliseconds(100));

        return response;
    }

    private HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string content = null)
    {
        var response = req.CreateResponse(statusCode);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString(content ?? statusCode.ToString());

        return response;
    }


}
