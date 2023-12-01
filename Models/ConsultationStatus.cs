namespace CloudHospital.UnreadMessageReminderJob.Models;

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
