using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class SearchICD10
{
    private readonly ILogger _logger;

    public SearchICD10(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SearchICD10>();
    }

    [Function("SearchICD10")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<SearchRequest>(requestBody);

        // For now, return a dummy response
        var response = req.CreateResponse(HttpStatusCode.OK);

        var dummyResult = new
        {
            Query = input?.Query,
            Suggestions = new[] {
                new { Code = "J20.9", Description = "Acute bronchitis, unspecified" }
            }
        };

        await response.WriteAsJsonAsync(dummyResult);
        return response;
    }
}

public class SearchRequest
{
    public string? Query { get; set; }
}
