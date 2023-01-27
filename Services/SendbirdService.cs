using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudHospital.UnreadMessageReminderJob.Services;

public class SendbirdService
{
    public const string DEFAULT_MEDIA_TYPE = "application/json";
    public readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;

    private readonly SendbirdConfiguration _sendbirdConfiguration;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ILogger _logger;

    public SendbirdService(
        IOptionsMonitor<SendbirdConfiguration> sendbirdConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
    {
        _sendbirdConfiguration = sendbirdConfigurationAccessor.CurrentValue;
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<SendbirdService>();
    }

    public async Task InviteGroupChannelV3Async(string channelUrl, InviteAsMembersModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelUrl))
        {
            throw new SendbirdApiException("invitation channel url is required");
        }

        // https://sendbird.com/docs/chat/v3/platform-api/channel/inviting-a-user/invite-as-members-channel
        var url = $"{GetBaseUrl()}/v3/group_channels/{Uri.EscapeDataString(channelUrl)}/invite";
        var client = CreateHttpClient();
        var httpRequestMessage = CreateHttpRequestMessage(HttpMethod.Post, url, model);

        _logger.LogInformation(@"Request: 
ApiKey: {ApiKey}        
ChannelUrl: {ChannelUrl}
Payload: {Json}
        ", string.IsNullOrWhiteSpace(_sendbirdConfiguration.ApiKey) ? "Not set" : "*****",
        channelUrl,
        JsonSerializer.Serialize(model, _jsonSerializerOptions));

        var response = await client.SendAsync(httpRequestMessage, cancellationToken: cancellationToken);

        var responseBodyString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Fail to invite user: {message}", responseBodyString);
            throw new SendbirdApiException($"Fail to invite user: {responseBodyString}");
        }

        _logger.LogInformation("User invitation succeed.");
    }

    public async Task LeaveGroupChannelV3Async(string channelUrl, LeaveMembersGroupChannelModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelUrl))
        {
            throw new SendbirdApiException("Channel url that leaves is required");
        }

        // https://sendbird.com/docs/chat/v3/platform-api/channel/managing-a-channel/leave-a-channel
        var url = $"{GetBaseUrl()}/v3/group_channels/{Uri.EscapeDataString(channelUrl)}/leave";
        var client = CreateHttpClient();
        var httpRequestMessage = CreateHttpRequestMessage(HttpMethod.Put, url, model);

        _logger.LogInformation(@"Request: 
ApiKey: {ApiKey}        
ChannelUrl: {ChannelUrl}
Payload: {Json}
        ", string.IsNullOrWhiteSpace(_sendbirdConfiguration.ApiKey) ? "Not set" : "*****",
        channelUrl,
        JsonSerializer.Serialize(model, _jsonSerializerOptions));

        var response = await client.SendAsync(httpRequestMessage, cancellationToken: cancellationToken);

        var responseBodyString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Fail to leave user: {message}", responseBodyString);
            throw new SendbirdApiException($"Fail to leave user: {responseBodyString}");
        }

        _logger.LogInformation("User to leave group channel succeed.");
    }

    private HttpClient CreateHttpClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(5),
    };

    private HttpRequestMessage CreateHttpRequestMessage<TPayload>(HttpMethod method, string url, TPayload? payload = null)
        where TPayload : class, new()
    {
        if (string.IsNullOrWhiteSpace(_sendbirdConfiguration.ApiKey))
        {
            throw new SendbirdApiException("Sendbird api key does not configure");
        }

        HttpRequestMessage request = new(method, url);

        request.Headers.Add("Api-Token", _sendbirdConfiguration.ApiKey);

        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);

            if (!string.IsNullOrWhiteSpace(json))
            {
                request.Content = new StringContent(json, Encoding.UTF8, DEFAULT_MEDIA_TYPE);
            }
        }

        return request;
    }

    private HttpRequestMessage CreateHttpRequestMessage(HttpMethod method, string url)
    {
        return CreateHttpRequestMessage(method, url, (object?)null);
    }

    private string GetBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(_sendbirdConfiguration.AppId))
        {
            throw new SendbirdApiException("Sendbird app id does not configure");
        }

        return $"https://api-{_sendbirdConfiguration.AppId}.sendbird.com";
    }
}

public class SendbirdConfiguration
{
    public string ApiKey { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;
}

public class SendbirdApiException : Exception
{
    public SendbirdApiException(string message) : base(message)
    {

    }
}

public class InviteAsMembersModel
{
    [JsonPropertyName("user_ids")]
    public IEnumerable<string> UserIds { get; set; } = Enumerable.Empty<string>();
}

public class LeaveMembersGroupChannelModel
{
    [JsonPropertyName("user_ids")]
    public IEnumerable<string> UserIds { get; set; } = Enumerable.Empty<string>();
}