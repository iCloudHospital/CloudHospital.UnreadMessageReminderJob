using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob;
using CloudHospital.UnreadMessageReminderJob.Converters;
using CloudHospital.UnreadMessageReminderJob.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
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

        services.AddTransient<EmailSender>();
    })
    .Build();

host.Run();
