using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob;
using CloudHospital.UnreadMessageReminderJob.Converters;
using CloudHospital.UnreadMessageReminderJob.Options;
using CloudHospital.UnreadMessageReminderJob.Services;
using Microsoft.Extensions.Configuration;
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

        services.AddOptions<NotificationApiConfiguration>()
            .Configure((options) =>
            {
                var notificationApiEnabled = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_API_ENABLED);

                if (!bool.TryParse(notificationApiEnabled, out bool notificationApiEnabledValue))
                {
                    notificationApiEnabledValue = false;
                }
                var oidcName = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_API_OIDC_NAME);
                var apiName = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_API_NAME);
                var baseUrl = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_API_BASE_URL);

                if (notificationApiEnabledValue)
                {
                    if (string.IsNullOrWhiteSpace(oidcName))
                    {
                        throw new ArgumentException("Notification api oidc name does not configure. Please check application settings.");
                    }
                    if (string.IsNullOrWhiteSpace(apiName))
                    {
                        throw new ArgumentException("Notification api name does not configure. Please check application settings.");
                    }
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        throw new ArgumentException("Notification api base url does not configure. Please check application settings.");
                    }
                }

                options.OidcName = oidcName;
                options.ApiName = apiName;
                options.BaseUrl = baseUrl;
                options.Enabled = notificationApiEnabledValue;
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

        services.AddTransient<EmailSender>();
        services.AddTransient<NotificationService>();

        services.AddHttpClient(Constants.HTTP_CLIENT_NOTIFICATION_API, (serviceProvider, client) =>
        {
            var baseUrl = Environment.GetEnvironmentVariable(Constants.ENV_NOTIFICATION_API_BASE_URL);
            client.BaseAddress = new Uri(baseUrl);

            // TODO: How to set user's access token
            string bearerToken = null;
            // var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            // var bearerToken = httpContextAccessor.HttpContext?.Request
            //     .Headers["Authorization"]
            //     .FirstOrDefault(header => header.StartsWith("bearer ", StringComparison.InvariantCultureIgnoreCase));

            // Add authorization if found
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                client.DefaultRequestHeaders.Add("Authorization", bearerToken);
            }
        });
    })
    .Build();

host.Run();
