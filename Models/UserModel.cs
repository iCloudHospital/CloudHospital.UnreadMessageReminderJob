using CloudHospital.UnreadMessageReminderJob.Services;

namespace CloudHospital.UnreadMessageReminderJob.Models;

public class UserModel
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string FullName { get => $"{FirstName} {LastName}"; }
}

public class DeviceModel
{
    public string UserId { get; set; }

    public Platform Platform { get; set; }
}