namespace CloudHospital.UnreadMessageReminderJob;

/// <summary>
/// Email template data model for unreadMessageReminder
/// </summary>
/// <example>
/// <code>
///     {
///        "Logo": "https://chblob.icloudhospital.com/assets/logo.png",
///        "WebsiteUrl": "https://minaps.icloudhospital.com",
///        "HospitalName": "@HospitalName",
///        "To": "test-to-user",
///        "From": "test-from-user",
///        "Message": "test-unread-messages",
///        "Created": "07-Dec-20 02:00pm",
///        "TargetPage": "@TargetPage"
///    }
/// </code>
/// </example>
public class UnreadMessageReminderEmialTemplateData
{
    public string Logo { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime? Created { get; set; } = DateTime.UtcNow;

    public string TargetPage { get; set; } = string.Empty;
}
