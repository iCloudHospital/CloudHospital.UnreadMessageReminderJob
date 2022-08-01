using System;
using System.Data.SqlClient;
using CloudHospital.UnreadMessageReminderJob.Models;
using CloudHospital.UnreadMessageReminderJob.Services;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CloudHospital.UnreadMessageReminderJob;

public class SendUnreadMessageReminder : FunctionBase
{
    private readonly EmailSender _emailSender;
    private readonly ILogger _logger;

    public SendUnreadMessageReminder(
        EmailSender emailSender,
        ILoggerFactory loggerFactory)
        : base()
    {
        _emailSender = emailSender;
        _logger = loggerFactory.CreateLogger<SendUnreadMessageReminder>();
    }

    [Function("SendUnreadMessageReminder")]
    public async Task Run(
        [QueueTrigger("%QueueName%%Stage%", Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
            SendBirdGroupChannelMessageSendEventModel item)
    {
        _logger.LogInformation($"⚡️ Dequeue item: {nameof(SendBirdGroupChannelMessageSendEventModel.Channel.ChannelUrl)}={item.Channel.ChannelUrl} {nameof(SendBirdGroupChannelMessageSendEventModel.Payload.MessageId)}={item.Payload.MessageId}");
        if (IsInDebug)
        {
            _logger.LogInformation($"🚀 {item.Payload?.Message} created at {item.Payload?.CreatedAt:HH:mm:ss}. now:{DateTime.UtcNow:HH:mm:ss}");
            _logger.LogInformation($"🔨 Email sender is ready: {_emailSender != null}");
        }

        // TODO: 사용자 타입 데이터를 어떤 필드에서 확인할 수 있는지 확인 필요
        // sender == [ChManager, Manager]

        // TODO: How to prepare template id and template data for sendgrid service 

        var member = item.Members
            .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Metadata?.UserType));

        //! find user email
        var userId = member.UserId;
        var user = await GetUser(userId);
        if (user == null)
        {
            if (IsInDebug)
            {
                _logger.LogWarning("User does not find");
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
            }
        }

        // user = new UserModel
        // {
        //     Id = Guid.NewGuid().ToString(),
        //     Email = "",
        //     FirstName = "",
        //     LastName = "",
        // };

        // send email using sendgrid template
        var templateData = GetSampleTemplateData(user.FullName, item.Sender.Nickname, item.Payload.Message, item.Payload.CreatedAt);
        if (IsInDebug && member.Nickname.Contains("PonCheol"))
        {
            // Send email 
            await _emailSender.SendEmailAsync(user.Email, user.FullName, EmailTemplateIds.UnreadMessage, templateData);
        }
    }

    private async Task<UserModel> GetUser(string id)
    {
        var connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_SQL_DATABASE_CONNECTION);

        using (var connection = new SqlConnection(connectionString))
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
AND A.IsHidden = 0
                ";

                var users = connection.Query<UserModel>(query, new { Id = id });

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
}

public class UserModel
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string FullName { get => $"{FirstName} {LastName}"; }
}
