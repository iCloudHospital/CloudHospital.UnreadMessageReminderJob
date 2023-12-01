using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using CloudHospital.UnreadMessageReminderJob.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class SendUnreadMessageReminder : FunctionBase
{
    private readonly EmailSender _emailSender;
    private readonly DatabaseService _databaseService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly SendbirdService _sendbirdService;
    private readonly ILogger _logger;

    public SendUnreadMessageReminder(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        EmailSender emailSender,
        DatabaseService databaseService,
        SendbirdService sendbirdService,
        ILoggerFactory loggerFactory)
        : base(debugConfigurationAccessor)
    {
        _emailSender = emailSender;
        _databaseService = databaseService;
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _sendbirdService = sendbirdService;
        _logger = loggerFactory.CreateLogger<SendUnreadMessageReminder>();
    }

    [Function(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_TRIGGER)]
    public async Task Run(
        [QueueTrigger(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
            SendBirdGroupChannelMessageSendEventModel item)
    {
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));
        var cancellationToken = cancellationTokenSource.Token;

        var logMessage = string.Empty;
        _logger.LogInformation($"⚡️ Dequeue item: {nameof(SendBirdGroupChannelMessageSendEventModel.Channel.ChannelUrl)}={item.Channel.ChannelUrl} {nameof(SendBirdGroupChannelMessageSendEventModel.Payload.MessageId)}={item.Payload.MessageId}");

        if (IsInDebug)
        {
            _logger.LogInformation($"🚀 {item.Payload?.Message} created at {item.Payload?.CreatedAt:HH:mm:ss}. now:{DateTime.UtcNow:HH:mm:ss}");
            _logger.LogInformation($"🔨 Email sender is ready: {_emailSender != null}");
        }

        if (_emailSender == null)
        {
            logMessage = "Email sender service is not configured. Please check app configuration.";
            _logger.LogWarning(logMessage);

            throw new Exception(logMessage);
        }

        var emailTemplateId = Environment.GetEnvironmentVariable(Constants.ENV_UNREAD_MESSAGE_REMINDER_SENDGRID_TEMPLATE_ID);

        if (string.IsNullOrWhiteSpace(emailTemplateId))
        {
            logMessage = "Unread message reminder email template does not set. Please check app configuration.";
            _logger.LogWarning(logMessage);

            throw new Exception(logMessage);
        }

        var senderUserType = item.Sender?.Metadata?.UserType;
        var senderUserId = item.Sender?.UserId;
        var members = item.Members
            .Where(x => x.UserId != senderUserId);

        if (IsInDebug && !members.Any())
        {
            _logger.LogInformation("⚠️ Members who will receive notification does not found.");
        }

        foreach (var member in members)
        {
            // find user email
            var userId = member?.UserId;
            var hospitalId = item.Channel.CustomType;

            var userTypeForTemplate = member?.Metadata?.UserType;

            if (string.IsNullOrWhiteSpace(userTypeForTemplate))
            {
                userTypeForTemplate = senderUserType;
            }

            if (IsInDebug)
            {
                _logger.LogInformation("🔨 userId={userId}, hospitalId={hospitalId}, userTypeForTemplate={userType}", userId, hospitalId, userTypeForTemplate);
            }

            var user = await _databaseService.GetUser(userId);
            var hospital = await _databaseService.GetHospitalAsync(hospitalId);

            if (user == null)
            {
                if (IsInDebug)
                {
                    _logger.LogWarning("User does not find. (userId={userId})", userId);
                }
            }
            else if (hospital == null)
            {
                if (IsInDebug)
                {
                    _logger.LogWarning("Hospital does not find. (hospitalId={hospitalID})", hospitalId);
                }
            }
            else
            {
                if (IsInDebug)
                {
                    _logger.LogInformation(@$"🔨 User:
{nameof(UserModel.Id)}={user.Id}
{nameof(UserModel.Email)}={user.Email}
{nameof(UserModel.FullName)}={user.FullName}
");
                    _logger.LogInformation(@"🔨 Hospital: 
Name: {name}
Logo: {logo}
website: {website}
", hospital.Name, hospital.Logo, hospital.WebsiteUrl);
                }

                // send email using sendgrid template
                var message = item.Payload?.Message ?? "You have unread chat message. Please check your message our application.";

                var cloudHospitalBaseUrl = Environment.GetEnvironmentVariable(Constants.ENV_CLOUDHOSPITAL_BASEURL);
                var targetPage = $"{cloudHospitalBaseUrl}/?chat=true";

                var templateData = GetTemplateData(userTypeForTemplate, user, item, hospital, message, targetPage);

                if (templateData == null)
                {
                    if (IsInDebug)
                    {
                        _logger.LogInformation($"❌ Not supported userType [userType={senderUserType}]");
                    }
                }
                else
                {
                    if (IsInDebug)
                    {
                        _logger.LogInformation(@"🔨 Email Template data: 
{json}
", JsonSerializer.Serialize(templateData, _jsonSerializerOptions));
                    }

                    await _emailSender.SendEmailAsync(user.Email, user.FullName, emailTemplateId, templateData, cancellationToken);

                    // Do not leave hospital manager in the group channel, if Sender is not patient #38
                    if (IsManagerUserType(item.Sender?.Metadata?.UserType))
                    {
                        // Hospital manager will leave in the channel #35
                        await LeaveGroupChannelAsync(item, cancellationToken);
                    }
                    else
                    {
                        if (IsInDebug)
                        {
                            _logger.LogInformation("✅ Sender is patient. Hospital manager remains in the group channel.");
                        }
                    }

                    if (IsInDebug)
                    {
                        _logger.LogInformation("✅ Unread message reminder job completed.");
                    }
                }
            }
        }
    }

    private object? GetTemplateData(string userTypeForTemplate, UserModel user, SendBirdGroupChannelMessageSendEventModel item, HospitalModel hospital, string? message = null, string? targetPage = null) => userTypeForTemplate switch
    {
        SendBirdSenderUserTypes.ChManager => GetChManagerTemplateData(user, item, hospital, message, targetPage),
        SendBirdSenderUserTypes.Manager => GetManagerTemplateData(user, item, hospital, message, targetPage),
        _ => null,
    };

    /// <summary>
    /// ChManager
    /// </summary>
    /// <param name="user"></param>
    /// <param name="item"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private UnreadMessageReminderEmialTemplateData GetChManagerTemplateData(UserModel user, SendBirdGroupChannelMessageSendEventModel item, HospitalModel hospital, string? message = null, string? targetPage = null)
    {
        var logoUrl = Environment.GetEnvironmentVariable(Constants.ENV_LOGO_IMAGE_URL);
        var cloudHospitalUrl = Environment.GetEnvironmentVariable(Constants.ENV_CLOUDHOSPITAL_BASEURL);

        var templateData = new UnreadMessageReminderEmialTemplateData
        {
            Logo = logoUrl,
            WebsiteUrl = cloudHospitalUrl,
            HospitalName = hospital.Name,
            To = user.FullName,
            From = item.Sender.Nickname,
            Message = message ?? item.Payload.Message,
            Created = item.Payload?.CreatedAt,
            TargetPage = targetPage,
        };

        return templateData;
    }

    /// <summary>
    /// Manager 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="item"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private UnreadMessageReminderEmialTemplateData GetManagerTemplateData(UserModel user, SendBirdGroupChannelMessageSendEventModel item, HospitalModel hospital, string? message = null, string? targetPage = null)
    {
        // Manager
        var templateData = new UnreadMessageReminderEmialTemplateData
        {
            Logo = hospital.Logo,
            WebsiteUrl = hospital.WebsiteUrl,
            HospitalName = hospital.Name,
            To = user.FullName,
            From = item.Sender.Nickname,
            Message = message ?? item.Payload.Message,
            Created = item.Payload?.CreatedAt,
            TargetPage = targetPage,
        };

        return templateData;
    }

    private async Task LeaveGroupChannelAsync(SendBirdGroupChannelMessageSendEventModel item, CancellationToken cancellationToken = default)
    {
        var managerIds = item.Members
            .Where(member => IsManagerUserType(member.Metadata?.UserType))
            .Select(member => member.UserId)
            .ToArray();

        try
        {
            await _sendbirdService.LeaveGroupChannelV3Async(item.Channel.ChannelUrl, new LeaveMembersGroupChannelModel
            {
                UserIds = managerIds,
            }, cancellationToken);

            if (IsInDebug)
            {
                _logger.LogInformation("✅ hospital manager leaves in the group channel. channel={channelUrl};manager={managerId}",
                    item.Channel.ChannelUrl,
                    string.Join(", ", managerIds));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Fail to leave manager to group channel. channel={channelUrl};manager={managerId};message={message}",
                item.Channel.ChannelUrl,
                string.Join(", ", managerIds),
                ex.Message);
        }
    }

    private bool IsManagerUserType(string? userType)
    {
        if (string.IsNullOrWhiteSpace(userType))
        {
            return false;
        }

        if (userType.Equals(SendBirdSenderUserTypes.ChManager, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (userType.Equals(SendBirdSenderUserTypes.Manager, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
