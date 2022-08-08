namespace CloudHospital.UnreadMessageReminderJob;

public class Constants
{
    // Name of environment variable
    public const string ENV_STAGE = "Stage";
    public const string ENV_SENDGRID_APIKEY = "SendGridApiKey";
    public const string ENV_SENDGRID_SENDER_EMAIL = "SendGridSenderEmail";
    public const string ENV_SENDGRID_SENDER_NAME = "SendGridSenderName";
    public const string ENV_UNREAD_MESSAGE_REMINDER_QUEUE_NAME = "UnreadMessageReminderQueueName";
    public const string ENV_UNREAD_MESSAGE_REMINDER_TABLE_NAME = "UnreadMessageReminderTableName";
    public const string ENV_UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE = "UnreadMessageReminderTimerSchedule";
    public const string ENV_UNREAD_DELAY_MINUTES = "UnreadDelayMinutes";
    public const string ENV_SENDBIRD_API_KEY = "SendBirdApiKey";
    public const string ENV_DEBUG = "Debug";

    // Unread message reminder
    public const string UNREAD_MESSAGE_REMINDER_QUEUE_NAME = $"%{ENV_UNREAD_MESSAGE_REMINDER_QUEUE_NAME}%%{ENV_STAGE}%";
    public const string UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE = $"%{ENV_UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE}%";
    public const string AZURE_STORAGE_ACCOUNT_CONNECTION = "AzureWebJobsStorage";



    public const string AZURE_SQL_DATABASE_CONNECTION = "Database";

    public const string REQUEST_HEAD_SENDBIRD_SIGNATURE = "x-sendbird-signature";

    // Functions names
    public const string UNREAD_MESSAGE_REMINDER_HTTP_TRIGGER = "GroupChannelMessageWebHook";
    public const string UNREAD_MESSAGE_REMINDER_TIMER_TRIGGER = "UnreadMessageReminderTimerJob";
    public const string UNREAD_MESSAGE_REMINDER_QUEUE_TRIGGER = "UnreadMessageReminderSendMessage";
}