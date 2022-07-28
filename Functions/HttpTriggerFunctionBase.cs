using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;

namespace CloudHospital.UnreadMessageReminderJob;

public abstract class HttpTriggerFunctionBase
{
    protected HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string content = null)
    {
        var response = req.CreateResponse(statusCode);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString(content ?? statusCode.ToString());

        return response;
    }

    protected async Task<TableClient> GetTableClient()
    {
        var storageAccountConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_STORAGE_ACCOUNT_CONNECTION);

        var tableClient = new TableClient(storageAccountConnectionString, Constants.TABLE_NAME);
        await tableClient.CreateIfNotExistsAsync();

        return tableClient;
    }
}