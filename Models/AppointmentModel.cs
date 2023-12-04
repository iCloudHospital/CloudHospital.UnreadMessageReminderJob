namespace CloudHospital.UnreadMessageReminderJob.Models;

public class AppointmentModel : ICallingReminderModel
{
    public string Id { get; set; } = string.Empty;

    public AppointmentType AppointmentType { get; set; }

    public string PatientId { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public string HospitalId { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string HospitalWebsiteUrl { get; set; } = string.Empty;

    public string? HospitalSpecialtyId { get; set; }

    public string? HospitalSpecialtyName { get; set; }

    public string? DoctorAffiliationId { get; set; }

    public string? DealPackageId { get; set; }

    public string? DealPackageName { get; set; }

    public string? ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public string Email { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; }

    public bool IsOnline { get; set; }

    public bool IsExternal { get; set; }

    public DateTime? ConfirmedDateStart { get; set; }

    public bool IsOpen { get; set; }
}
