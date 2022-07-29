using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace CloudHospital.UnreadMessageReminderJob;

public abstract class HttpTriggerFunctionBase : FunctionBase
{
    protected HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string content = null)
    {
        var response = req.CreateResponse(statusCode);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString(content ?? statusCode.ToString());

        return response;
    }
}
