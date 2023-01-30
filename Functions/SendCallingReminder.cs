using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using CloudHospital.UnreadMessageReminderJob.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class SendCallingReminder : FunctionBase
{

    private readonly NotificationService _notificationService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger _logger;

    public SendCallingReminder(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        NotificationService notificationService,
        DatabaseService databaseService,
        ILoggerFactory loggerFactory)
        : base(debugConfigurationAccessor)
    {
        _notificationService = notificationService;
        _databaseService = databaseService;
        _logger = loggerFactory.CreateLogger<SendCallingReminder>();
    }

    [Function(Constants.CALLING_REMINDER_QUEUE_TRIGGER)]
    public async Task Run(
        [QueueTrigger(Constants.CALLING_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
        ConsultationModel item)
    {
        _logger.LogInformation($"⚡️ Dequeue item: {nameof(ConsultationModel.Id)}={item.Id}");

        var cancellationTokenSource = new CancellationTokenSource();

        // query consultation
        var consultation = await _databaseService.GetConsultationAsync(item.Id);

        if (consultation == null || !consultation.IsOpen || consultation.Status != ConsultationStatus.Paid)
        {
            var isNull = consultation == null;
            var isClosed = !consultation?.IsOpen;
            var isNotPaid = consultation?.Status != ConsultationStatus.Paid;

            _logger.LogInformation("Notification ignored: {@Reason}", new { isNull, isClosed, isNotPaid });
            return;
        }

        // prevent duplicate nontifications
        var notificationExists = await _databaseService.HasNotificationConsultationRelatedAsync(item.Id);

        if (notificationExists)
        {
            _logger.LogInformation("Notification duplicated: {@Request}", new { item.Id });
            return;
        }

        // managers
        List<UserModel> managers = await _databaseService.GetManagersAsync(!string.IsNullOrWhiteSpace(item.HospitalWebsiteUrl), item.HospitalId);

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

            _logger.LogInformation("✅ Notification send requested.");
        }
        catch (Exception ex)
        {
            cancellationTokenSource.Cancel();

            _logger.LogWarning(ex, ex.Message);
        }
    }
}
