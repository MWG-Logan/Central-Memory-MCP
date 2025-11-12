using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CentralMemoryMcp.Functions.Functions;

public class HealthFunctions
{
    [Function("Health")] 
    public HttpResponseData Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.WriteString("OK");
        return res;
    }

    [Function("Ready")] 
    public HttpResponseData Ready([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ready")] HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.WriteString("READY");
        return res;
    }
}
