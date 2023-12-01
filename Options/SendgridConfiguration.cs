namespace CloudHospital.UnreadMessageReminderJob.Options;

public class SendgridConfiguration
{
    public string SourceEmail { get; set; }
    public string SourceName { get; set; }
    public string ApiKey { get; set; }
    public bool SandboxMode { get; set; }
}

/// <summary>
/// Template list
/// </summary>
/// <remarks>
/// https://mc.sendgrid.com/dynamic-templates
/// </remarks>
// public static class EmailTemplateIds
// {
//     public const string UnreadMessage = "d-ae813a9bdc0d4ad39ab6adae3bcbae19";
// }
