namespace CloudHospital.UnreadMessageReminderJob.Services;

public class NotificationRequestModel
{
    public string NotificationCode { get; set; }

    public string NotificationTargetId { get; set; }

    public string? ReceiverId { get; set; }

    public string Message { get; set; }
}