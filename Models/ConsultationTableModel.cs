using Azure;
using Azure.Data.Tables;

namespace CloudHospital.UnreadMessageReminderJob.Models;

public class ConsultationTableModel : ITableEntity
{
    /// <summary>
    /// Consultation.Id
    /// </summary>
    public string PartitionKey { get; set; }
    /// <summary>
    /// new guid
    /// </summary>
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Consultation.ConfirmedDateStart
    /// </summary>
    public DateTime ConfirmedDateStart { get; set; }

    public string Json { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}

public enum ConsultationType : byte
{
    Hospital,
    Doctor,
    Deal
}