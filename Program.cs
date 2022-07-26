using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob;
using CloudHospital.UnreadMessageReminderJob.Options;
using CloudHospital.UnreadMessageReminderJob.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddOptions<DatabaseConfiguration>()
        .Configure(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_SQL_DATABASE_CONNECTION);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Database connection string does not configure. Please check application settings.");
            }

            options.ConnectionString = connectionString;
        });

        services
            .AddOptions<JsonSerializerOptions>()
            .Configure(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNameCaseInsensitive = false;
            });

        services.AddOptions<SendgridConfiguration>()
            .Configure(options =>
            {
                var sendGridApiKey = Environment.GetEnvironmentVariable(Constants.ENV_SENDGRID_APIKEY);
                var sendGridSenderEmail = Environment.GetEnvironmentVariable(Constants.ENV_SENDGRID_SENDER_EMAIL);
                var sendGridSenderName = Environment.GetEnvironmentVariable(Constants.ENV_SENDGRID_SENDER_NAME);

                if (string.IsNullOrWhiteSpace(sendGridApiKey))
                {
                    throw new ArgumentException("Sendgrid api key does not configure. Please check application settings.");
                }

                options.ApiKey = sendGridApiKey;
                options.SourceEmail = sendGridSenderEmail;
                options.SourceName = sendGridSenderName;
            });

        services.AddOptions<AzureNotificationHubsConfiguration>()
            .Configure(options =>
            {
                var notificationHubConnectionString = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_HUB_CONNECTION_STRING);
                var notificationHubName = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_HUB_NAME);

                if (string.IsNullOrWhiteSpace(notificationHubConnectionString))
                {
                    throw new ArgumentException("Azure Notification Hub connection string does not configure. Please check application settings.");
                }

                if (string.IsNullOrWhiteSpace(notificationHubName))
                {
                    throw new ArgumentException("Azure Notification Hub name string does not configure. Please check application settings.");
                }

                options.AccessSignature = notificationHubConnectionString;
                options.HubName = notificationHubName;
            });

        services.AddOptions<DebugConfiguration>()
            .Configure(options =>
            {
                var isInDebug = Environment.GetEnvironmentVariable(Constants.ENV_DEBUG);
                if (!bool.TryParse(isInDebug, out bool isInDebugValue))
                {
                    isInDebugValue = false;
                }

                options.IsInDebug = isInDebugValue;
            });

        services.AddTransient<EmailSender>();
        services.AddTransient<NotificationService>();
    })
    .Build();

host.Run();
