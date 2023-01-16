namespace CloudHospital.UnreadMessageReminderJob.Models;

public class ConsultationModel
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public DateTime ConfirmedDateStart { get; set; }
    public ConsultationType? ConsultationType { get; set; }

    public string HospitalId { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalWebsiteUrl { get; set; } = string.Empty;

    public ConsultationStatus? Status { get; set; }

    public bool IsOpen { get; set; }
}

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