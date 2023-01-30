namespace CloudHospital.UnreadMessageReminderJob.Options;

public class DebugConfiguration
{
    public bool IsInDebug { get; set; } = false;

    public bool BypassPayloadValidation { get; set; } = false;
}
