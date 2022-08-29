using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Options;
using CloudHospital.UnreadMessageReminderJob.Services;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class SendUnreadMessageReminder : FunctionBase
{
    private readonly EmailSender _emailSender;
    private readonly DatabaseConfiguration _databaseConfiguration;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ILogger _logger;

    public SendUnreadMessageReminder(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor,
        IOptionsMonitor<DatabaseConfiguration> databaseConfigurationAccessor,
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        EmailSender emailSender,
        ILoggerFactory loggerFactory)
        : base(debugConfigurationAccessor)
    {
        _emailSender = emailSender;
        _databaseConfiguration = databaseConfigurationAccessor.CurrentValue;
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<SendUnreadMessageReminder>();
    }

    [Function(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_TRIGGER)]
    public async Task Run(
        [QueueTrigger(Constants.UNREAD_MESSAGE_REMINDER_QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
            SendBirdGroupChannelMessageSendEventModel item)
    {
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

        // sender == [ChManager, Manager]
        var userType = item.Sender?.Metadata?.UserType;

        var member = item.Members
            .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Metadata?.UserType));

        if (IsInDebug && member == null)
        {
            _logger.LogInformation("Member does not found.");
        }

        // find user email
        var userId = member?.UserId;
        var hospitalId = item.Channel.CustomType;

        if (IsInDebug)
        {
            _logger.LogInformation("🔨 userId={userId}, hospitalId={hospitalId}", userId, hospitalId);
        }

        var user = await GetUser(userId);
        var hospital = await GetHospitalAsync(hospitalId);

        if (user == null)
        {
            if (IsInDebug)
            {
                _logger.LogWarning("User does not find.");
            }
        }
        else if (hospital == null)
        {
            if (IsInDebug)
            {
                _logger.LogWarning("Hospital does not find.");
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

            var templateData = GetTemplateData(user, item, hospital, message, targetPage);

            if (templateData == null)
            {
                _logger.LogInformation($"Not supported userType [userType={userType}]");
            }
            else
            {

                if (IsInDebug)
                {
                    _logger.LogInformation(@$"🔨 Email Template data: 
{JsonSerializer.Serialize(templateData, _jsonSerializerOptions)}
");
                }

                await _emailSender.SendEmailAsync(user.Email, user.FullName, emailTemplateId, templateData);

                _logger.LogInformation("✅ Unread message reminder job completed.");
            }

        }

    }

    private async Task<UserModel?> GetUser(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("User id is empty.");

            return null;
        }



        using (var connection = new SqlConnection(_databaseConfiguration.ConnectionString))
        {
            try
            {
                await connection.OpenAsync();
                var query = @"
SELECT
    top 1
    Id,
    FirstName,
    LastName,
    Email
FROM
    Users U 
INNER JOIN
    UserAuditableEntities A 
ON
    U.Id = A.UserId    
WHERE
    U.Id = @Id 
AND A.IsDeleted = 0
                ";

                var users = await connection.QueryAsync<UserModel>(query, new { Id = id });

                if (users != null && users.Any())
                {
                    var foundUser = users.FirstOrDefault();
                    if (foundUser != null)
                    {
                        return foundUser;
                    }
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        return null;
    }

    private async Task<HospitalModel?> GetHospitalAsync(string hospitalId)
    {
        using (var connection = new SqlConnection(_databaseConfiguration.ConnectionString))
        {
            var query = @"
SELECT 
    h.Id,
    h.Logo,
    h.WebsiteUrl,
    t.Name
FROM 
    Hospitals h 
INNER JOIN 
    HospitalAuditableEntities auditable
ON
    h.Id = auditable.HospitalId
INNER JOIN 
    HospitalTranslations t 
ON
    h.Id = t.HospitalId
and t.LanguageCode = 'en'
where 
    auditable.IsDeleted = 0
AND h.Id = @HospitalId 
            ";

            var hospitals = await connection.QueryAsync<HospitalModel>(query, new { HospitalId = hospitalId });

            return hospitals.FirstOrDefault();
        }
    }

    [Obsolete("Use Channel.CustomType")]
    private string? GetHospitalIdFromChannelUrl(string channelUrl)
    {
        // channel_url 
        // CH: {hospitalId}--{userId}
        // 9b722461-dfa0-42ba-d069-08d7f631e33c--5d945b83-83fd-434c-baba-9aac88014c85
        // 
        // Etc: external--{hospitalId}--{userId}
        // external--bae1b5bc-325d-45fb-6b91-08d93a6658e7--970aaa4d-0997-4b4d-bc04-df07f6a17906

        var tokens = channelUrl.Split("--", StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length > 2)
        {
            return tokens[1];
        }
        else if (tokens.Length > 1)
        {
            return tokens[0];
        }
        else
        {
            return null;
        }
    }

    private object GetSampleTemplateData(string toName, string fromName, string message, DateTime? created)
    {
        var templateData = new
        {
            To = toName,
            From = fromName,
            Message = message,
            Created = created
        };

        return templateData;
    }

    private object? GetTemplateData(UserModel user, SendBirdGroupChannelMessageSendEventModel item, HospitalModel hospital, string? message = null, string? targetPage = null) => item?.Sender?.Metadata?.UserType switch
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
}


public class UnreadMessageReminderEmialTemplateData
{
    /*
    {
        "Logo": "https://chblob.icloudhospital.com/assets/logo.png",
        "WebsiteUrl": "https://minaps.icloudhospital.com",
        "HospitalName": "@HospitalName",
        "To": "test-to-user",
        "From": "test-from-user",
        "Message": "test-unread-messages",
        "Created": "07-Dec-20 02:00pm",
        "TargetPage": "@TargetPage"
    }
    */
    public string Logo { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime? Created { get; set; } = DateTime.UtcNow;
    public string TargetPage { get; set; } = string.Empty;
}
