using System;
using System.Data.SqlClient;
using CloudHospital.UnreadMessageReminderJob.Models;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CloudHospital.UnreadMessageReminderJob
{
    public class SendUnreadMessageReminder
    {
        private readonly ILogger _logger;

        public SendUnreadMessageReminder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SendUnreadMessageReminder>();
        }

        [Function("SendUnreadMessageReminder")]
        public async Task Run(
            [QueueTrigger(Constants.QUEUE_NAME, Connection = Constants.AZURE_STORAGE_ACCOUNT_CONNECTION)]
            EventModel item)
        {
            _logger.LogInformation($"⚡️ Dequeue item: {nameof(EventModel.GroupId)}={item.GroupId} {nameof(EventModel.Id)}={item.Id}");

            //await Task.Delay(TimeSpan.FromMilliseconds(100));

            _logger.LogInformation($"🚀 {item.Message} created at {item.Created:HH:mm:ss}. now:{DateTimeOffset.UtcNow:HH:mm:ss}");

            // find user email
            var userId = "001b7d3c-3a9a-4204-bf27-b094aa7c9cef";
            var user = await GetUser(userId);
            if (user == null)
            {
                _logger.LogWarning("User does not find");
            }
            else
            {
                _logger.LogInformation(@$"🔨 User:
{nameof(User.Id)}={user.Id}
{nameof(User.Email)}={user.Email}
{nameof(User.FullName)}={user.FullName}
");
            }

            // send email using sendgrid template
        }

        private async Task<User> GetUser(string id)
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

                    var users = connection.Query<User>(query, new { Id = id });

                    if (users != null && users.Count() > 0)
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
    }

    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName { get => $"{FirstName} {LastName}"; }
    }
}
