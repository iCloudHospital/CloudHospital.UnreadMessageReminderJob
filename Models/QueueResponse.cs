using Microsoft.Azure.Functions.Worker;

namespace CloudHospital.UnreadMessageReminderJob.Models;

/// <summary>
/// Queue output binding wrapper
/// </summary>
/// <typeparam name="T"></typeparam>
public class QueueResponse<T>
{
    [QueueOutput(Constants.QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
    public List<T> Items { get; set; } = new List<T>();
}