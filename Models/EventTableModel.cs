using Azure;
using Azure.Data.Tables;

namespace CloudHospital.UnreadMessageReminderJob.Models;

public class EventTableModel : ITableEntity
{
    /// <summary>
    /// chat group identifier
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// row identifier
    /// </summary>
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Payload from http trigger
    /// </summary>
    public string Json { get; set; } = string.Empty;

    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
