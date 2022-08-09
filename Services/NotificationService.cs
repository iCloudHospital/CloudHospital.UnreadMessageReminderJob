using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloudHospital.UnreadMessageReminderJob.Models;
using Dapper;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob.Services;

public class NotificationService
{
    private readonly NotificationApiConfiguration _notificationApiConfiguration;
    private readonly HttpClient _httpClient;
    private readonly NotificationHubClient _notificationHubClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        IOptions<NotificationApiConfiguration> notificationApiConfiguration,
        IOptions<AzureNotificationHubsConfiguration> azureNotificationHubsConfiguration,
        ILogger<NotificationService> logger
        )
    {
        _notificationApiConfiguration = notificationApiConfiguration.Value ?? new NotificationApiConfiguration();
        _httpClient = httpClientFactory.CreateClient("notification");
        _notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(azureNotificationHubsConfiguration.Value.AccessSignature, azureNotificationHubsConfiguration.Value.HubName);
        _logger = logger;
    }

    public async Task<NotificationModel> SendNotificationAsync(string title, NotificationModel notification, CancellationToken cancellationToken = default)
    {
        // Insert notification 
        int effected = 0;
        var connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_SQL_DATABASE_CONNECTION);
        var queryInsertNotification = @"
INSERT INTO Notifications
(
    Id,
    NotificationCode,
    NotificationTargetId,
    SenderId,
    ReceiverId,
    Message,
    CreatedAt,
    IsChecked,
    IsDeleted
)
VALUES 
(
    @Id,
    @NotificationCode,
    @NotificationTargetId,
    @SenderId,
    @ReceiverId,
    @Message,
    @CreatedAt,
    @IsChecked,
    @IsDeleted
)
        ";
        using (var connection = new SqlConnection(connectionString))
        {
            effected = await connection.ExecuteAsync(queryInsertNotification, new
            {
                Id = notification.Id,
                NotificationCode = notification.NotificationCode,
                NotificationTargetId = notification.NotificationTargetId,
                SenderId = notification.SenderId,
                ReceiverId = notification.ReceiverId,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                IsChecked = notification.IsChecked,
                IsDeleted = notification.IsDeleted,

            });
        }

        var succeed = effected > 0;
        if (succeed)
        {
            // var devices = await _context.Devices
            //     .Where(x => x.AuditableEntity != null && x.AuditableEntity.IsDeleted == false && x.AuditableEntity.IsHidden == false)
            //     .Where(x => x.UserId == notification.ReceiverId)
            //     .ToListAsync(cancellationToken);

            var queryDevices = @"
SELECT
    device.UserId,
    device.Platform
FROM
    Devices device
INNER JOIN 
    DeviceAuditableEntities auditable
ON
    device.Id = auditable.DeviceId
WHERE
    auditable.IsDeleted = 0
AND auditable.IsHidden  = 0
AND device.UserId       = @UserId
";

            List<DeviceModel> devices = new();
            using (var connection = new SqlConnection(connectionString))
            {
                var result = await connection.QueryAsync<DeviceModel>(queryDevices, new { UserId = notification.ReceiverId });
                devices = result.ToList();
            }

            var templateData = await GetTemplateDataByNotificationId(notification.Id);

            var deviceGroups = devices.GroupBy(x => new { x.UserId, x.Platform });
            foreach (var deviceGroup in deviceGroups)
            {
                var tags = new List<string> { "$userId:{" + deviceGroup.Key.UserId + "}" };
                tags.Add(string.Format("AppAlert && {0}", NotificationType.EventAlert.ToString()));

                await SendPushNotificationAsync(tags, deviceGroup.Key.Platform, title, notification.Message, templateData, cancellationToken);
            }

            await SendNotificationAsync(notification);
        }

        return notification;
    }

    protected async Task<PushNotificationTemplate> GetTemplateDataByNotificationId(string notificationId, CancellationToken cancellationToken = default)
    {
        // TODO: Query notifications
        // var notification = await _context.Notifications
        //     .Include(x => x.Sender)
        //     .Include(x => x.Receiver)
        //     .Where(x => x.Id == notificationId)
        //     .FirstOrDefaultAsync(cancellationToken);

        var connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_SQL_DATABASE_CONNECTION);
        var query = @"
SELECT
    notification.Id,
    notification.NotificationCode,
    notification.NotificationTargetId,
    notification.SenderId,
    notification.ReceiverId,
    notification.Message,
    notification.CreatedAt,
    notification.IsChecked,
    sender.FirstName,
    sender.LastName,
    sender.Email,
    receiver.FirstName,
    receiver.LastName,
    receiver.Email
FROM
    Notifications as notification
LEFT JOIN 
    Users as sender
ON 
    notification.SenderId = sender.Id
LEFT JOIN 
    Users as receiver
ON 
    notification.ReceiverId = receiver.Id
WHERE
    notification.Id = @Id
";
        NotificationModel notification = null;
        using (var connection = new SqlConnection(connectionString))
        {
            var notifications = await connection
                .QueryAsync<NotificationModel, UserModel, UserModel, NotificationModel>(
                    query,
                    (notification, sender, receiver) =>
                    {
                        notification.Sender = sender;
                        notification.Receiver = receiver;

                        return notification;
                    }, new { Id = notificationId },
                    splitOn: "Id, SenderId, ReceiverId");

            notification = notifications?.FirstOrDefault();
        }

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

    protected async Task<PushNotificationTemplate> GetTemplateDataByNotificationId(NotificationModel notification, CancellationToken token)
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

    private async Task SendNotificationAsync(NotificationModel model, CancellationToken cancellationToken = default)
    {
        if (!_notificationApiConfiguration.Enabled || string.IsNullOrWhiteSpace(model.ReceiverId) || string.IsNullOrWhiteSpace(model.ReceiverId))
        {
            return;
        }

        var baseUrl = _notificationApiConfiguration.BaseUrl;
        var path = "/api/v1/notifications";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{path}");
        requestMessage.Content = CreateHttpContent(model);

        try
        {
            var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception("Error response from notification center");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fail to send notification: {ex.Message}");
        }
    }

    private HttpContent CreateHttpContent(NotificationModel notification)
    {
        var model = new NotificationRequestModel
        {
            NotificationCode = notification.NotificationCode.ToString(),
            NotificationTargetId = notification.NotificationTargetId,
            ReceiverId = notification.ReceiverId,
            Message = notification.Message
        };

        var payload = JsonSerializer.Serialize(model);
        return new StringContent(payload, Encoding.UTF8, "application/json");
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