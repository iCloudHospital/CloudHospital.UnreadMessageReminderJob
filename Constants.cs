namespace CloudHospital.UnreadMessageReminderJob;

public class Constants
{
    // Name of environment variable
    public const string ENV_STAGE = "Stage";
    /// <summary>
    /// CloudHospital logo url
    /// </summary>
    public const string ENV_LOGO_IMAGE_URL = "LogoImageUrl";
    public const string ENV_CLOUDHOSPITAL_BASEURL = "CloudHospitalBaseUrl";
    public const string ENV_SENDGRID_APIKEY = "SendGridApiKey";
    public const string ENV_SENDGRID_SENDER_EMAIL = "SendGridSenderEmail";
    public const string ENV_SENDGRID_SENDER_NAME = "SendGridSenderName";
    public const string ENV_UNREAD_MESSAGE_REMINDER_QUEUE_NAME = "UnreadMessageReminderQueueName";
    public const string ENV_UNREAD_MESSAGE_REMINDER_TABLE_NAME = "UnreadMessageReminderTableName";
    public const string ENV_UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE = "UnreadMessageReminderTimerSchedule";
    public const string ENV_UNREAD_DELAY_MINUTES = "UnreadDelayMinutes";
    public const string ENV_UNREAD_MESSAGE_REMINDER_SENDGRID_TEMPLATE_ID = "UnreadMessageReminderSendGridTemplateId";
    public const string ENV_CALLING_REMINDER_TABLE_NAME = "CallingReminderTableName";
    public const string ENV_CALLING_REMINDER_QUEUE_NAME = "CallingReminderQueueName";
    public const string ENV_CALLING_REMINDER_TIMER_SCHEDULE = "CallingReminderTimerSchedule";
    public const string ENV_CALLING_REMINDER_BASIS = "CallingReminderBasis";

    public const string ENV_SENDBIRD_API_KEY = "SendBirdApiKey";
    public const string ENV_SENDBIRD_APP_ID = "SendBirdAppId";

    public const string ENV_NOTIFICATION_HUB_CONNECTION_STRING = "NotificationHubConnectionString";
    public const string ENV_NOTIFICATION_HUB_NAME = "NotificationHubName";

    public const string ENV_HELP_USER_ID = "HelpUserId";

    public const string ENV_DEBUG = "Debug";

    public const string ENV_BypassPayloadValidation = "BypassPayloadValidation";


    // Unread message reminder
    public const string UNREAD_MESSAGE_REMINDER_QUEUE_NAME = $"%{ENV_UNREAD_MESSAGE_REMINDER_QUEUE_NAME}%%{ENV_STAGE}%";
    public const string UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE = $"%{ENV_UNREAD_MESSAGE_REMINDER_TIMER_SCHEDULE}%";

    public const string AZURE_STORAGE_ACCOUNT_CONNECTION = "AzureWebJobsStorage";

    // Calling reminder
    public const string CALLING_REMINDER_QUEUE_NAME = $"%{ENV_CALLING_REMINDER_QUEUE_NAME}%%{ENV_STAGE}%";
    public const string CALLING_REMINDER_TIMER_SCHEDULE = $"%{ENV_CALLING_REMINDER_TIMER_SCHEDULE}%";

    // http clients
    public const string HTTP_CLIENT_NOTIFICATION_API = "notification";


    public const string AZURE_SQL_DATABASE_CONNECTION = "Database";

    public const string REQUEST_HEAD_SENDBIRD_SIGNATURE = "x-sendbird-signature";

    // Functions names
    public const string UNREAD_MESSAGE_REMINDER_HTTP_TRIGGER = "GroupChannelMessageWebHook";
    public const string UNREAD_MESSAGE_REMINDER_TIMER_TRIGGER = "UnreadMessageReminderTimerJob";
    public const string UNREAD_MESSAGE_REMINDER_QUEUE_TRIGGER = "UnreadMessageReminderSendMessage";


    public const string CALLING_REMINDER_HTTP_TRIGGER = "CallingReminderConsultationUpdatedWebHook";
    public const string CALLING_REMINDER_TIMER_TRIGGER = "CallingReminderTimerJob";
    public const string CALLING_REMINDER_QUEUE_TRIGGER = "CallingReminderSendNotification";


}