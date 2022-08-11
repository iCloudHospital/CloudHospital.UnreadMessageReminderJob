namespace CloudHospital.UnreadMessageReminderJob.Models;

public class ConsultationModel
{
    public string Id { get; set; }
    public string PatientId { get; set; }
    public DateTime ConfirmedDateStart { get; set; }
    public ConsultationType ConsultationType { get; set; }

    public string HospitalId { get; set; }
    public string HospitalName { get; set; }
    public string HospitalWebsiteUrl { get; set; }

    public bool IsOpen { get; set; }
}
