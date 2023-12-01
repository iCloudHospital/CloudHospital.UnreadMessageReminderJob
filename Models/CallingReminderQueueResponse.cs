using Microsoft.Azure.Functions.Worker;

namespace CloudHospital.UnreadMessageReminderJob.Models;

/// <summary>
/// Queue output binding wrapper
/// </summary>
public class CallingReminderQueueResponse
{
    [QueueOutput(Constants.CALLING_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
    public List<ConsultationModel> Items { get; set; } = new();
}