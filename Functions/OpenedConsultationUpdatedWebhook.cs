using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public class OpenedConsultationUpdatedWebhook : HttpTriggerFunctionBase
{
    public OpenedConsultationUpdatedWebhook(
        IOptionsMonitor<JsonSerializerOptions> jsonSerializerOptionsAccessor,
        ILoggerFactory loggerFactory)
    {
        _jsonSerializerOptions = jsonSerializerOptionsAccessor.CurrentValue;
        _logger = loggerFactory.CreateLogger<OpenedConsultationUpdatedWebhook>();
    }

    [Function(Constants.OPENED_CONSULTATION_UPDATE_HTTP_TRIGGER)]
    public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
    {
        _logger.LogInformation($"⚡️ [{nameof(OpenedConsultationUpdatedWebhook)}] HTTP trigger function processed a request.");

        var response = CreateResponse(req, HttpStatusCode.OK);

        var payload = string.Empty;

        if (req.Body == null || req.Body.Length == 0)
        {
            _logger.LogWarning("Payload is empty.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }

        byte[] payloadBinary = null;
        using (var memoryStream = new MemoryStream())
        {
            req.Body.Position = 0;
            await req.Body.CopyToAsync(memoryStream);
            await req.Body.FlushAsync();

            payloadBinary = memoryStream.ToArray();

            memoryStream.Position = 0;
            using (var reader = new StreamReader(memoryStream))
            {
                payload = await reader.ReadToEndAsync();
                reader.Close();
            }
        }

        if (IsInDebug)
        {
            _logger.LogInformation($"Payload: {payload}");
        }

        if (string.IsNullOrWhiteSpace(payload) || payloadBinary == null || payloadBinary.Length == 0)
        {
            _logger.LogWarning("Payload is empty.");
            return CreateResponse(req, HttpStatusCode.BadRequest);
        }



        return response;
    }

    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
}

