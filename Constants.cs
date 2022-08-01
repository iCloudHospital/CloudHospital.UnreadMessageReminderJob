namespace CloudHospital.UnreadMessageReminderJob;

public class Constants
{
    public const string ENV_STAGE = "Stage";
    public const string ENV_SENDGRID_APIKEY = "SendGridApiKey";
    public const string ENV_SENDGRID_SENDER_EMAIL = "SendGridSenderEmail";
    public const string ENV_SENDGRID_SENDER_NAME = "SendGridSenderName";
    public const string ENV_QUEUE_NAME = "QueueName";
    public const string ENV_TABLE_NAME = "TableName";
    public const string ENV_TIMER_SCHEDULE = "TimerSchedule";
    public const string ENV_UNREAD_DELAY_MINUTES = "UnreadDelayMinutes";
    public const string ENV_SENDBIRD_API_KEY = "SendBirdApiKey";
    public const string ENV_DEBUG = "Debug";

    // public const string QUEUE_NAME = "sendbirdmessages";

    /// <summary>
    /// The name must be written in alphanumeric characters. It must start and end with alphabet character.
    /// </summary>
    // public const string TABLE_NAME = "sendbirdmessages";

    public const string AZURE_STORAGE_ACCOUNT_CONNECTION = "AzureWebJobsStorage";

    public const string AZURE_SQL_DATABASE_CONNECTION = "Database";

    // public const int DELAYED_MIN = 2;

    public const string REQUEST_HEAD_SENDBIRD_SIGNATURE = "x-sendbird-signature";
}