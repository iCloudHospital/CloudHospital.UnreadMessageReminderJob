namespace CloudHospital.UnreadMessageReminderJob;

public class Constants
{
    public const string ENV_SENDGRID_APIKEY = "SendGridApiKey";
    public const string ENV_SENDGRID_SENDER_EMAIL = "SendGridSenderEmail";
    public const string ENV_SENDGRID_SENDER_NAME = "SendGridSenderName";

    public const string QUEUE_NAME = "sendbird-messages";

    public const string TABLE_NAME = "test";

    public const string AZURE_STORAGE_ACCOUNT_CONNECTION = "AzureWebJobsStorage";

    public const string AZURE_SQL_DATABASE_CONNECTION = "Database";

    public const int DELAYED_MIN = 2;

    public const string TIMER_SCHEDULE = "0 */1 * * * *";
}