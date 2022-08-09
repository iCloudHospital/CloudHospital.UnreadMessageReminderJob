using System.Data.SqlClient;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using CloudHospital.UnreadMessageReminderJob.Services;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class SendCallingReminder : FunctionBase
{
    public SendCallingReminder(
        NotificationService notificationService,
        IOptionsMonitor<DatabaseConfiguration> databaseConfigurationAccessor,
        ILoggerFactory loggerFactory)
        : base()
    {
        _notificationService = notificationService;
        _databaseConfiguration = databaseConfigurationAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<SendUnreadMessageReminder>();
    }

    [Function(Constants.CALLING_REMINDER_QUEUE_TRIGGER)]
    public async Task Run(
        [QueueTrigger(Constants.CALLING_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
        ConsultationModel item)
    {
        _logger.LogInformation($"⚡️ Dequeue item: {nameof(ConsultationModel.Id)}={item.Id}");

        var cancellationTokenSource = new CancellationTokenSource();

        // query consultation
        var queryConsultation = @"
SELECT
    top 1 
    consultation.Id,
    consultation.ConsultationType,
    consultation.[Status],
    consultation.IsOpen
FROM
    Consultations consultation
WHERE
    consultation.IsDeleted = 0
AND consultation.IsHidden = 0
AND consultation.Id = @Id 
";

        Consultation consultation = null;
        using (var connection = new SqlConnection(_databaseConfiguration.ConnectionString))
        {
            consultation = await connection.QueryFirstOrDefaultAsync<Consultation>(queryConsultation, new { Id = item.Id });
        }

        if (consultation == null || consultation.IsOpen || consultation.Status != ConsultationStatus.Paid)
        {
            var isNull = consultation == null;
            var isClosed = !consultation?.IsOpen;
            var isNotPaid = consultation?.Status != ConsultationStatus.Paid;

            _logger.LogInformation("Notification ignored: {@Reason}", new { isNull, isClosed, isNotPaid });
            return;
        }

        // TODO: prevent duplicate nontifications
        var notificationExists = false;
        var queryNotification = @"
SELECT
    top 1
    Id,
    NotificationCode
FROM 
    Notifications
WHERE 
    NotificationCode = @NotificationCode 
AND Id               = @Id
    
";

        using (var connection = new SqlConnection(_databaseConfiguration.ConnectionString))
        {
            var notifications = await connection.QueryAsync<NotificationModel>(queryNotification, new
            {
                Id = item.Id,
                NotificationCode = NotificationCode.ConsultationReady,
            });

            notificationExists = notifications.Any();
        }

        if (notificationExists)
        {
            _logger.LogInformation("Notification duplicated: {@Request}", new { item.Id });
            return;
        }

        // users
        List<UserModel> managers = new();
        IEnumerable<UserModel> queryResults = Enumerable.Empty<UserModel>();
        using (var connection = new SqlConnection(_databaseConfiguration.ConnectionString))
        {
            string queryForManager = string.Empty;
            if (string.IsNullOrWhiteSpace(item.HospitalWebsiteUrl))
            {
                // chmanagers (UserType == 5)
                queryForManager = @"
SELECT 
    u1.Id,
    u1.LastName,
    u1.FirstName,
    '' as Email
FROM 
    Users u1
INNER JOIN 
    UserAuditableEntities a1
ON 
    u1.Id = a1.UserId
Where 
    u1.UserType = 5
AND a1.IsDeleted = 0
AND a1.IsHidden = 0
";
                queryResults = await connection.QueryAsync<UserModel>(queryForManager);


            }
            else
            {
                // managers with hospitalId (UserType == 4)
                queryForManager = @"
SELECT 
    u1.Id,
    u1.LastName,
    u1.FirstName,
    '' as Email
FROM 
    Users u1
INNER JOIN 
    UserAuditableEntities a1
ON 
    u1.Id = a1.UserId
WHERE 
    u1.UserType = 4
AND a1.IsDeleted = 0
AND a1.IsHidden = 0
AND EXISTS 
        (
            SELECT Id 
            FROM ManagerAffiliations m1
            WHERE m1.ManagerId = u1.Id
              and m1.HospitalId = @HospitalId
        ) 
";

                queryResults = await connection.QueryAsync<UserModel>(queryForManager, new { HospitalId = item.HospitalId });
            }

            managers = queryResults.ToList();
        }

        // Send push notification to user
        try
        {
            var titleForUser = "Consultation start now";
            var messageForUser = string.Format("Check your scheduled {0} consultation from {1}", item.ConsultationType.ToString().ToLower(), item.HospitalName);

            var notification = new NotificationModel
            {
                NotificationCode = NotificationCode.ConsultationReady,
                NotificationTargetId = consultation.Id,
                ReceiverId = item.PatientId,
                Message = messageForUser,
                CreatedAt = DateTime.UtcNow,
            };

            await _notificationService.SendNotificationAsync(titleForUser, notification, cancellationTokenSource.Token);

            // Send push notification to manager

            var messageForManager = "Consultation start now.";
            foreach (var manager in managers)
            {
                var notificationForManager = new NotificationModel
                {
                    NotificationCode = NotificationCode.ConsultationRefunded,
                    NotificationTargetId = consultation.Id,
                    ReceiverId = manager.Id,
                    Message = messageForManager,
                    CreatedAt = DateTime.UtcNow,
                };

                await _notificationService.SendNotificationAsync(messageForManager, notificationForManager, cancellationTokenSource.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, ex.Message);
            cancellationTokenSource.Cancel();
        }
    }

    private readonly NotificationService _notificationService;
    private readonly DatabaseConfiguration _databaseConfiguration;

    private readonly ILogger _logger;
}

public class Consultation
{
    public string Id { get; set; }
    public ConsultationType ConsultationType { get; set; }
    public ConsultationStatus Status { get; set; }
    public bool IsOpen { get; set; }
}

public enum ConsultationStatus : byte
{
    New,
    Rejected,
    Approved,
    Paid,
    Canceled,
    RefundRequested,
    Refunded,
}