using CloudHospital.UnreadMessageReminderJob.Models;
using Microsoft.Azure.Functions.Worker;

namespace CloudHospital.UnreadMessageReminderJob;

/// <summary>
/// Queue output binding wrapper
/// </summary>
public class UnreadMessageReminderQueueResponse
{
    [QueueOutput(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
    public List<SendBirdGroupChannelMessageSendEventModel> Items { get; set; } = new();
}
