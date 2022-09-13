using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

/// <summary>
/// sendbird webhook
/// </summary>
public class GroupChannelMessageWebHook : HttpTriggerFunctionBase
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GroupChannelMessageWebHook(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
        : base(debugConfigurationAccessor)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<GroupChannelMessageWebHook>();
    }

    [Function(Constants.UNREAD_MESSAGE_REMINDER_HTTP_TRIGGER)]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData req)
    {
        _logger.LogInformation($"⚡️ [{nameof(GroupChannelMessageWebHook)}] HTTP trigger function processed a request.");

        SendBirdGroupChannelEventModel model = null;

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

        try
        {
            var verifiedRequest = VerifySendBirdSignature(req, payloadBinary, IsInDebug);

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
    }

    private async Task<HttpResponseData> ProcessGroupChannelMessageRead(HttpRequestData req, string payload)
    {
        _logger.LogInformation($"⚡️ [group_channel:message_read] HTTP trigger function processed a request.");

        SendBirdGroupChannelMessageReadEventModel model = null;

        var response = CreateResponse(req, HttpStatusCode.OK);

        try
        {
            model = JsonSerializer.Deserialize<SendBirdGroupChannelMessageReadEventModel>(payload, _jsonSerializerOptions);
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
        // When Reader == User
        var tableName = GetTableNameForUnreadMessageReminder();
        var tableClient = await GetTableClient(tableName);

        var queryResult = tableClient.QueryAsync<EventTableModel>(filter: $"{nameof(EventTableModel.PartitionKey)} eq '{model.Channel.ChannelUrl}'").AsPages();
        try
        {
            await foreach (var result in queryResult)
            {
                foreach (var item in result.Values)
                {
                    try
                    {
                        var sendEventModel = JsonSerializer.Deserialize<SendBirdGroupChannelMessageSendEventModel>(item.Json, _jsonSerializerOptions);

                        if (sendEventModel == null)
                        {
                            throw new Exception($"Fail to deserialize SendEventModel. [{item.PartitionKey}:{item.RowKey}]");
                        }

                        // Reader is not sender
                        if (ConfirmToRead(sendEventModel.Sender.UserId, model))
                        {
                            await tableClient.DeleteEntityAsync(item.PartitionKey, item.RowKey);

                            if (IsInDebug)
                            {
                                _logger.LogInformation($"Remove old data. {nameof(EventTableModel.PartitionKey)}={model.Channel.ChannelUrl}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, ex.Message);

                        throw;
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {

            return CreateResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task<HttpResponseData> ProcessGroupChannelMessageSend(HttpRequestData req, string payload)
    {
        _logger.LogInformation($"⚡️ [group_channel:message_send] HTTP trigger function processed a request.");

        SendBirdGroupChannelMessageSendEventModel model = null;

        var response = CreateResponse(req, HttpStatusCode.OK);

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
        }

        if (!IsMessageToProcess(model))
        {
            // target userType is one of [CHManager, Manager]
            _logger.LogInformation("✅ Done. The message that does not concerned is not saving.");
            return response;
        }

        var entry = new EventTableModel
        {
            PartitionKey = model.Channel.ChannelUrl,
            RowKey = Guid.NewGuid().ToString(),
            Message = string.Empty,
            Json = payload,
            Created = DateTime.UtcNow, //model.Payload.CreatedAt,
        };

        // SendBirdGroupChannelMessageSendEventModel.Sender 에 따라 처리할 내용이 다릅니다.
        // When sender: one of [ChManager, Manager]
        var tableName = GetTableNameForUnreadMessageReminder();
        var tableClient = await GetTableClient(tableName);

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

    private bool VerifySendBirdSignature(HttpRequestData req, byte[] signatureRawData, bool isInDebug = false)
    {
        var sendbirdApiKey = Environment.GetEnvironmentVariable(Constants.ENV_SENDBIRD_API_KEY);

        if (string.IsNullOrWhiteSpace(sendbirdApiKey))
        {
            if (isInDebug)
            {
                _logger.LogWarning("Api key is not configured");
            }
            throw new ArgumentException("Api key is not configured");
        }

        if (req.Headers == null || !req.Headers.Contains(Constants.REQUEST_HEAD_SENDBIRD_SIGNATURE))
        {
            if (isInDebug)
            {
                _logger.LogWarning("x-signature header does not exists");
            }
            throw new ArgumentException("x-signature header does not exists");
        }

        var values = Enumerable.Empty<string>();

        if (!req.Headers.TryGetValues(Constants.REQUEST_HEAD_SENDBIRD_SIGNATURE, out values))
        {
            if (isInDebug)
            {
                _logger.LogWarning("x-signature header does not have the value. E001");
            }
            throw new ArgumentException("x-signature header does not have the value. E001");
        }

        var signature = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(signature))
        {
            if (isInDebug)
            {
                _logger.LogWarning("x-signature header does not have the value. E002");
            }
            throw new ArgumentException("x-signature header does not have the value. E002");
        }

        //byte[] signatureRawData = Encoding.UTF8.GetBytes(payload);
        var keyBinaries = Encoding.UTF8.GetBytes(sendbirdApiKey);
        var hashedString = string.Empty;

        using (var hmac = new HMACSHA256(keyBinaries))
        {
            var hashedValue = hmac.ComputeHash(signatureRawData);
            hashedString = string.Join("", hashedValue.Select(x => x.ToString("x2")));
        }

        var authorizedRequest = signature.Equals(hashedString, StringComparison.OrdinalIgnoreCase);

        if (isInDebug)
        {
            _logger.LogInformation(@$"Verification:
Header value: {signature}
Hashed value: {hashedString}
");
            _logger.LogInformation($"Request is authorized: {authorizedRequest}");
        }

        return authorizedRequest;
    }

    /// <summary>
    /// Whether to save the message depends on who sent it.
    /// <list>
    ///     <item>Sender is CHManager</item>
    ///     <item>Sender is Manager</item>
    ///     <item>Sender is User and receiver is not Manager</item>
    /// </list>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private bool IsMessageToProcess(SendBirdGroupChannelMessageSendEventModel model)
    {
        var senderIsUser = string.IsNullOrWhiteSpace(model.Sender.Metadata?.UserType);
        if (senderIsUser)
        {
            // sender is User and receiver is Manager (Not CHManager)
            var toManager = model.Members
                .Where(model => !string.IsNullOrWhiteSpace(model.Metadata?.UserType) && model.Metadata?.UserType == SendBirdSenderUserTypes.Manager)
                .Any();

            if (toManager)
            {
                return false;
            }
        }

        return true;
    }

    private bool ConfirmToRead(string senderUserId, SendBirdGroupChannelMessageReadEventModel model)
    {
        if (string.IsNullOrWhiteSpace(senderUserId))
        {
            throw new ArgumentException($"{nameof(senderUserId)} does not allow null or empty string.", nameof(senderUserId));
        }

        if (model == null)
        {
            throw new ArgumentException($"{model} does not allow null.", nameof(model));
        }

        return model.ReadUpdates.Any(x => x.UserId != senderUserId);
    }
}

