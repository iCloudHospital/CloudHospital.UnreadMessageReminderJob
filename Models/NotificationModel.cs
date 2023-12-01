namespace CloudHospital.UnreadMessageReminderJob.Models;

public class NotificationModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public NotificationCode NotificationCode { get; set; }

    public string NotificationTargetId { get; set; } = string.Empty;

    public string? SenderId { get; set; }

    public UserModel? Sender { get; set; }

    public string? ReceiverId { get; set; }

    public UserModel? Receiver { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsChecked { get; set; } = false;

    public bool IsDeleted { get; set; } = false;
}
