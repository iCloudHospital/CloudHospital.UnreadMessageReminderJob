using System.Net;
using CloudHospital.UnreadMessageReminderJob.Options;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace CloudHospital.UnreadMessageReminderJob;

public abstract class HttpTriggerFunctionBase : FunctionBase
{
    public HttpTriggerFunctionBase(
        IOptionsMonitor<DebugConfiguration> debugConfigurationAccessor)
        : base(debugConfigurationAccessor)
    {

    }

    protected HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string content = null)
    {
        var response = req.CreateResponse(statusCode);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString(content ?? statusCode.ToString());

        return response;
    }
}
