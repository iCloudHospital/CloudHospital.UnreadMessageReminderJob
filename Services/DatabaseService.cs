using System.Data.SqlClient;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob.Services;

public class DatabaseService
{
    private readonly DatabaseConfiguration _databaseConfiguration;
    private readonly ILogger _logger;

    public DatabaseService(
        IOptionsMonitor<DatabaseConfiguration> databaseConfigurationAccessor,
        ILogger<DatabaseConfiguration> logger)
    {
        _databaseConfiguration = databaseConfigurationAccessor.CurrentValue;
        _logger = logger;
    }

    public async Task<List<DeviceModel>> GetDevicesAsync(string receiverId)
    {

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

        using var connection = new SqlConnection(_databaseConfiguration.ConnectionString);
        try
        {
            var result = await connection.QueryAsync<DeviceModel>(queryDevices, new { UserId = receiverId });

            _logger.LogInformation("🔨 devices query result={count}", result.Count());

            return result.ToList();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<int> InsertNotificationAsync(NotificationModel notification)
    {
        var affected = 0;
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
        using var connection = new SqlConnection(_databaseConfiguration.ConnectionString);

        try
        {
            affected = await connection.ExecuteAsync(queryInsertNotification, new
            {
                Id = notification.Id,
                NotificationCode = (int)notification.NotificationCode,
                NotificationTargetId = notification.NotificationTargetId,
                SenderId = notification.SenderId,
                ReceiverId = notification.ReceiverId,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                IsChecked = notification.IsChecked,
                IsDeleted = notification.IsDeleted,
            });


            _logger.LogInformation("🔨 Notification inserted. affected={affected}", affected);

            return affected;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<NotificationModel?> GetNotificationById(string notificationId)
    {
        var connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_SQL_DATABASE_CONNECTION);
        var query = @"
SELECT
    CONVERT(nchar(36), notification.Id) as Id,
    notification.NotificationCode,
    CONVERT(nchar(36), notification.NotificationTargetId) as NotificationTargetId,
    
    notification.Message,
    notification.CreatedAt,
    notification.IsChecked,
    
    notification.SenderId,
    sender.FirstName,
    sender.LastName,
    sender.Email,

    notification.ReceiverId,
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

        using var connection = new SqlConnection(connectionString);
        try
        {
            var notifications = await connection
                .QueryAsync<NotificationModel, UserModel, UserModel, NotificationModel>(
                    sql: query,
                    map: (notification, sender, receiver) =>
                    {
                        if (sender != null)
                        {
                            notification.Sender = sender;
                        }
                        if (receiver != null)
                        {
                            notification.Receiver = receiver;
                        }

                        return notification;
                    },
                    param: new { Id = notificationId },
                    splitOn: "Id, SenderId, ReceiverId");

            return notifications?.FirstOrDefault();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<UserModel?> GetUser(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("User id is empty.");

            return null;
        }

        using var connection = new SqlConnection(_databaseConfiguration.ConnectionString);

        try
        {
            await connection.OpenAsync();
            var query = @"
SELECT
    top 1
    Id,
    FirstName,
    LastName,
    Email
FROM
    Users U 
INNER JOIN
    UserAuditableEntities A 
ON
    U.Id = A.UserId    
WHERE
    U.Id = @Id 
AND A.IsDeleted = 0
                ";

            var users = await connection.QueryAsync<UserModel>(query, new { Id = id });

            if (users != null && users.Any())
            {
                var foundUser = users.FirstOrDefault();
                if (foundUser != null)
                {
                    return foundUser;
                }
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        return null;
    }

    public async Task<HospitalModel?> GetHospitalAsync(string hospitalId)
    {
        using var connection = new SqlConnection(_databaseConfiguration.ConnectionString);
        try
        {
            var query = @"
SELECT 
    h.Id,
    h.Logo,
    h.WebsiteUrl,
    t.Name
FROM 
    Hospitals h 
INNER JOIN 
    HospitalAuditableEntities auditable
ON
    h.Id = auditable.HospitalId
INNER JOIN 
    HospitalTranslations t 
ON
    h.Id = t.HospitalId
and t.LanguageCode = 'en'
where 
    auditable.IsDeleted = 0
AND h.Id = @HospitalId 
            ";

            var hospitals = await connection.QueryAsync<HospitalModel>(query, new { HospitalId = hospitalId });

            return hospitals.FirstOrDefault();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}