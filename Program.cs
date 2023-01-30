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
                var bypassPayloadValidation = Environment.GetEnvironmentVariable(Constants.ENV_BypassPayloadValidation);

                if (!bool.TryParse(isInDebug, out bool isInDebugValue))
                {
                    isInDebugValue = false;
                }

                if (!bool.TryParse(bypassPayloadValidation, out bool bypassPayloadValidationValue))
                {
                    bypassPayloadValidationValue = false;
                }

                options.IsInDebug = isInDebugValue;
                options.BypassPayloadValidation = bypassPayloadValidationValue;
            });

        services.AddOptions<SendbirdConfiguration>()
            .Configure(options =>
            {
                var sendbirdApiKey = Environment.GetEnvironmentVariable(Constants.ENV_SENDBIRD_API_KEY);
                var sendbirdAppId = Environment.GetEnvironmentVariable(Constants.ENV_SENDBIRD_APP_ID);

                if (string.IsNullOrWhiteSpace(sendbirdApiKey))
                {
                    throw new ArgumentException("Sendbird api key does not configure. Please check application settings.");
                }

                if (string.IsNullOrWhiteSpace(sendbirdAppId))
                {
                    throw new ArgumentException("Sendbird app id does not configure. Please check application settings.");
                }

                options.ApiKey = sendbirdApiKey;
                options.AppId = sendbirdAppId;
            });

        services.AddOptions<AccountConfiguration>()
            .Configure(options =>
            {
                var helpUserId = Environment.GetEnvironmentVariable(Constants.ENV_HELP_USER_ID);

                if (string.IsNullOrWhiteSpace(helpUserId))
                {
                    throw new ArgumentException("Help user id does not configure. Please check application settings.");
                }

                options.HelpUserId = helpUserId;
            });

        services.AddTransient<EmailSender>();
        services.AddTransient<NotificationService>();
        services.AddTransient<SendbirdService>();
        services.AddTransient<DatabaseService>();
    })
    .Build();

host.Run();
