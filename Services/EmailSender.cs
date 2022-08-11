
using CloudHospital.UnreadMessageReminderJob.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StrongGrid;
using StrongGrid.Models;

namespace CloudHospital.UnreadMessageReminderJob.Services;
public class EmailSender
{
    private readonly DebugConfiguration _debugConfiguration;
    private readonly SendgridConfiguration _configuration;
    private readonly ILogger _logger;

    public EmailSender(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<SendgridConfiguration> configuration, ILogger<EmailSender> logger)
    {
        _debugConfiguration = debugConfigurationAccessor.CurrentValue ?? new();
        _configuration = configuration.CurrentValue;
        _logger = logger;
    }

    public async Task<string> SendEmailAsync(string toEmail, string toName, string templateId, object templateData = null, CancellationToken cancellationToken = default)
    {
        var client = new Client(_configuration.ApiKey);

        var messageId = string.Empty;

        try
        {
            var unsubscribeOptions = new UnsubscribeOptions
            {
                GroupId = UnsubscribeGroupIds.Default
            };

            messageId = await client.Mail.SendToSingleRecipientAsync(
                to: new MailAddress(toEmail, toName),
                from: new MailAddress(_configuration.SourceEmail, _configuration.SourceName),
                dynamicTemplateId: templateId,
                dynamicData: templateData,
                unsubscribeOptions: unsubscribeOptions,
                mailSettings: new MailSettings
                {
                    SandboxModeEnabled = _configuration.SandboxMode
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sendgrid error");
        }

        if (!string.IsNullOrEmpty(messageId))
        {
            _logger.LogInformation("Email successfully sent: {@Param}", new { toEmail, templateId, templateData });
        }

        return messageId;
    }
}

public class SendgridConfiguration
{
    public string SourceEmail { get; set; }
    public string SourceName { get; set; }
    public string ApiKey { get; set; }
    public bool SandboxMode { get; set; }
}

/// <summary>
/// Unsubscribe Groups
/// </summary>
/// <remarks>
/// https://app.sendgrid.com/suppressions/advanced_suppression_manager
/// </remarks>
public static class UnsubscribeGroupIds
{
    public const long Default = 18772;
}

/// <summary>
/// Template list
/// </summary>
/// <remarks>
/// https://mc.sendgrid.com/dynamic-templates
/// </remarks>
public static class EmailTemplateIds
{
    public const string UnreadMessage = "d-ae813a9bdc0d4ad39ab6adae3bcbae19";
}
