using System.Text.Json;
using CloudHospital.UnreadMessageReminderJob.Converters;
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
    })
    .Build();

host.Run();
