namespace CloudHospital.UnreadMessageReminderJob;

public class StopTaskException : Exception
{
    public StopTaskException(string? message)
    : base(message) { }

    public StopTaskException(string? message, Exception? innerException)
    : base(message, innerException) { }
}