namespace CloudHospital.UnreadMessageReminderJob.Models;

public interface ICallingReminderModel
{
    /// <summary>
    /// Identifier of the entry
    /// </summary>
    public string Id { get; }

    public DateTime? ConfirmedDateStart { get; }

    /// <summary>
    /// Presents the entry is opened state or not
    /// </summary>
    public bool IsOpen { get; }
}
