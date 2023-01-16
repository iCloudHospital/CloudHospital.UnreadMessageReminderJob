using System.Text.Json;
using System.Text.Json.Serialization;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob.Services;

public class NotificationService
{
    private readonly NotificationHubClient _notificationHubClient;
    private readonly DebugConfiguration _debugConfiguration;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private readonly DatabaseService _databaseService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptions<AzureNotificationHubsConfiguration> azureNotificationHubsConfiguration,
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        DatabaseService databaseService,
        ILogger<NotificationService> logger
        )
    {
        _notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(azureNotificationHubsConfiguration.Value.AccessSignature, azureNotificationHubsConfiguration.Value.HubName);
        _debugConfiguration = debugConfigurationAccessor.CurrentValue ?? new();
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<NotificationModel> SendNotificationAsync(string title, NotificationModel notification, CancellationToken cancellationToken = default)
    {
        // Insert notification 
        int affected = await _databaseService.InsertNotificationAsync(notification);

        var succeed = affected > 0;
        if (succeed)
        {
            List<DeviceModel> devices = await _databaseService.GetDevicesAsync(notification.ReceiverId);

            var templateData = await GetTemplateDataByNotificationIdAsync(notification.Id);

            if (_debugConfiguration.IsInDebug)
            {
                _logger.LogInformation(@"🔨 Template Data: {json}", JsonSerializer.Serialize(templateData, _jsonSerializerOptions));
            }

            var deviceGroups = devices.GroupBy(x => new { x.UserId, x.Platform });
            foreach (var deviceGroup in deviceGroups)
            {
                var tags = new List<string> { "$userId:{" + deviceGroup.Key.UserId + "}" };
                tags.Add(string.Format("AppAlert && {0}", NotificationType.EventAlert.ToString()));

                await SendPushNotificationAsync(tags, deviceGroup.Key.Platform, title, notification.Message, templateData, cancellationToken);

                if (_debugConfiguration.IsInDebug)
                {
                    _logger.LogInformation("Request push notification. {notification}", JsonSerializer.Serialize(new
                    {
                        Tags = tags,
                        PlatForm = deviceGroup.Key.Platform,
                        Title = title,
                        Message = notification.Message,
                        TemplateData = templateData,
                    }, _jsonSerializerOptions));
                }
            }
        }

        return notification;
    }

    protected async Task<PushNotificationTemplate> GetTemplateDataByNotificationIdAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _databaseService.GetNotificationById(notificationId);

        if (notification == null)
        {
            return null;
        }

        var senderName = (notification.Sender != null) ? $"{notification.Sender.FirstName} {notification.Sender.LastName}" : null;
        var receiverName = (notification.Receiver != null) ? $"{notification.Receiver.FirstName} {notification.Receiver.LastName}" : null;

        return new PushNotificationTemplate
        {
            Id = notification.Id,
            NotificationCode = notification.NotificationCode,
            NotificationTargetId = notification.NotificationTargetId,
            SenderId = notification.SenderId,
            ReceiverId = notification.ReceiverId,
            Message = notification.Message,
            CreatedAt = notification.CreatedAt,
            IsChecked = notification.IsChecked,
            SenderName = senderName,
            ReceiverName = receiverName
        };
    }

    protected async Task<PushNotificationTemplate> GetTemplateDataByNotificationIdAsync(NotificationModel notification, CancellationToken token)
    {
        var senderName = (notification.Sender != null) ? $"{notification.Sender.FirstName} {notification.Sender.LastName}" : null;
        var receiverName = (notification.Receiver != null) ? $"{notification.Receiver.FirstName} {notification.Receiver.LastName}" : null;

        return new PushNotificationTemplate
        {
            Id = notification.Id,
            NotificationCode = notification.NotificationCode,
            NotificationTargetId = notification.NotificationTargetId,
            SenderId = notification.SenderId,
            ReceiverId = notification.ReceiverId,
            Message = notification.Message,
            CreatedAt = notification.CreatedAt,
            IsChecked = notification.IsChecked,
            SenderName = senderName,
            ReceiverName = receiverName
        };
    }

    private async Task SendPushNotificationAsync(List<string> tags, Platform platform, string title, string body, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonPayload = string.Empty;
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };

            var jsonData = JsonSerializer.Serialize(data, jsonOptions);

            NotificationOutcome outcome = null;

            switch (platform)
            {
                case Platform.iOS:
                    jsonPayload = "{\"aps\":{\"alert\": { \"title\":\"" + title + "\", \"body\":\"" + body + "\"}, \"sound\":\"default\"}, \"data\":" + jsonData + "}";
                    outcome = await _notificationHubClient.SendAppleNativeNotificationAsync(jsonPayload, tags, cancellationToken);
                    break;
                case Platform.Android:
                    jsonPayload = "{\"data\": {\"message\":\"" + title + "\", \"body\":\"" + body + "\"}, \"data\":" + jsonData + "}";
                    outcome = await _notificationHubClient.SendFcmNativeNotificationAsync(jsonPayload, tags, cancellationToken);
                    break;
            }

            if (outcome != null)
            {
                if (!((outcome.State == NotificationOutcomeState.Abandoned) || (outcome.State == NotificationOutcomeState.Unknown)))
                {
                    _logger.LogInformation("Notification successfully sent: {@Param}", new { tags, platform, title, body, data });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push Notification error");
        }
    }
}

public class NotificationApiConfiguration
{
    /// <summary>
    /// Default OidcName
    /// </summary>
    public string OidcName { get; set; }

    /// <summary>
    /// Default ApiName
    /// </summary>
    public string ApiName { get; set; }

    /// <summary>
    /// BaseUrl
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// Enabled
    /// </summary>
    public bool Enabled { get; set; }
}

public class AzureNotificationHubsConfiguration
{
    /// <summary>
    /// HubName
    /// </summary>
    public string HubName { get; set; }

    /// <summary>
    /// AccessSignature
    /// </summary>
    public string AccessSignature { get; set; }
}


public class NotificationClient
{
    public NotificationHubClient Instance { get; set; }

    public NotificationClient(IOptionsMonitor<AzureNotificationHubsConfiguration> configurationAccessor)
    {
        var configuration = configurationAccessor.CurrentValue;
        Instance = NotificationHubClient.CreateClientFromConnectionString(configuration.AccessSignature, configuration.HubName);
    }
}

public enum NotificationType : byte
{
    EventAlert,
    NoticeAlert
}

public enum Platform : byte
{
    Web,
    iOS,
    Android
}

public class PushNotificationTemplate
{
    public string Id { get; set; }

    public NotificationCode NotificationCode { get; set; }

    public string NotificationTargetId { get; set; }

    public string? SenderId { get; set; }

    public string? SenderName { get; set; }

    public string? ReceiverId { get; set; }

    public string? ReceiverName { get; set; }

    public string Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsChecked { get; set; }
}

public class NotificationRequestModel
{
    public string NotificationCode { get; set; }

    public string NotificationTargetId { get; set; }

    public string? ReceiverId { get; set; }

    public string Message { get; set; }
}
