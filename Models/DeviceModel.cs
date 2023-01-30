using CloudHospital.UnreadMessageReminderJob.Services;

namespace CloudHospital.UnreadMessageReminderJob.Models;

public class DeviceModel
{
    public string UserId { get; set; }

    public Platform Platform { get; set; }
}