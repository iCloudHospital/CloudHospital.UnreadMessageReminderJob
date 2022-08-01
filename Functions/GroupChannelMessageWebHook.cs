﻿using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class GroupChannelMessageWebHook : HttpTriggerFunctionBase
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    public GroupChannelMessageWebHook(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
        : base()
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<GroupChannelMessageWebHook>();
    }

    [Function("GroupChannelMessageWebHook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
    {
        _logger.LogInformation($"⚡️ [{nameof(GroupChannelMessageWebHook)}] HTTP trigger function processed a request.");

        SendBirdGroupChannelEventModel model = null;
        //
        var response = CreateResponse(req, HttpStatusCode.OK);

        var payload = string.Empty;

        if (req.Body == null || req.Body.Length == 0)
        {
            _logger.LogWarning("Payload is empty.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        req.Body.Position = 0;

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
            var verifiedRequest = VerifySendBirdSignature(req, payload);

            if (!verifiedRequest)
            {
                throw new Exception("Unauthorized request");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, ex.Message);

            return CreateResponse(req, HttpStatusCode.Unauthorized, ex.Message);
        }

        try
        {
            model = JsonSerializer.Deserialize<SendBirdGroupChannelEventModel>(payload, _jsonSerializerOptions);
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

        if (model.Category == SendBirdGroupChannelEventCategories.MessageRead)
        {
            return await ProcessGroupChannelMessageRead(req, payload);

        }
        else if (model.Category == SendBirdGroupChannelEventCategories.MessageSend)
        {
            return await ProcessGroupChannelMessageSend(req, payload);
        }
        else
        {
            _logger.LogWarning("Current event category is not supported.");

            return CreateResponse(req, HttpStatusCode.NotAcceptable);
        }

        return response;
    }

    private async Task<HttpResponseData> ProcessGroupChannelMessageRead(HttpRequestData req, string payload)
    {
        _logger.LogInformation($"⚡️ [group_channel:message_read] HTTP trigger function processed a request.");

        SendBirdGroupChannelMessageSendEventModel model = null;

        var response = CreateResponse(req, HttpStatusCode.OK);

        //var payload = string.Empty;

        //req.Body.Position = 0;

        //using (var reader = new StreamReader(req.Body))
        //{
        //    payload = await reader.ReadToEndAsync();
        //    reader.Close();
        //}

        //if (string.IsNullOrWhiteSpace(payload))
        //{
        //    _logger.LogWarning("Payload is empty.");
        //    return CreateResponse(req, HttpStatusCode.BadRequest);
        //}

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

        // SendBirdGroupChannelMessageSendEventModel.Sender 에 따라 처리할 내용이 다릅니다.
        // TODO: 사용자 타입 데이터를 어떤 필드에서 확인할 수 있는지 확인 필요
        // When Reader == User

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

        return response;
    }

    private async Task<HttpResponseData> ProcessGroupChannelMessageSend(HttpRequestData req, string payload)
    {
        _logger.LogInformation($"⚡️ [group_channel:message_send] HTTP trigger function processed a request.");

        SendBirdGroupChannelMessageSendEventModel model = null;

        var response = CreateResponse(req, HttpStatusCode.OK);

        //var payload = string.Empty;

        //req.Body.Position = 0;

        //using (var reader = new StreamReader(req.Body))
        //{
        //    payload = await reader.ReadToEndAsync();
        //    reader.Close();
        //}

        //if (string.IsNullOrWhiteSpace(payload))
        //{
        //    _logger.LogWarning("Payload is empty.");
        //    return CreateResponse(req, HttpStatusCode.BadRequest);
        //}

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

    private bool VerifySendBirdSignature(HttpRequestData req, string payload)
    {
        var sendbirdApiKey = Environment.GetEnvironmentVariable(Constants.ENV_SENDBIRD_API_KEY);

        if (string.IsNullOrWhiteSpace(sendbirdApiKey))
        {
            throw new ArgumentException("Api key is not configured");
        }

        if (req.Headers == null || !req.Headers.Contains(Constants.REQUEST_HEAD_SENDBIRD_SIGNATURE))
        {
            throw new ArgumentException("x-signature header does not exists");
        }

        var values = Enumerable.Empty<string>();

        if (!req.Headers.TryGetValues(Constants.REQUEST_HEAD_SENDBIRD_SIGNATURE, out values))
        {
            throw new ArgumentException("x-signature header does not have the value. E001");
        }

        var signature = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(signature))
        {
            throw new ArgumentException("x-signature header does not have the value. E002");
        }


        byte[] signatureRawData = Encoding.UTF8.GetBytes(payload);
        var keyBinaries = Encoding.UTF8.GetBytes(sendbirdApiKey);
        var hashedString = string.Empty;

        using (var hmac = new HMACSHA256(keyBinaries))
        {
            var hashedValue = hmac.ComputeHash(signatureRawData);
            hashedString = Convert.ToBase64String(hashedValue);
        }

        return signature.Equals(hashedString, StringComparison.Ordinal);
    }
}