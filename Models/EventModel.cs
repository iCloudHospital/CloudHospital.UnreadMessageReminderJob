using System.Text.Json.Serialization;

namespace CloudHospital.UnreadMessageReminderJob.Models;

public class EventModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string GroupId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime Created { get; set; } = DateTime.UtcNow;
}

