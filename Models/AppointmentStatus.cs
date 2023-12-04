namespace CloudHospital.UnreadMessageReminderJob.Models;

public enum AppointmentStatus
{
    New = 10,
    Rejected = 20,
    Approved = 30,
    Paid = 40,
    Canceled = 50,
    RefundRequested = 60,
    Refunded = 70
}
