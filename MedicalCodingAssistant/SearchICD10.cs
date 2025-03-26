using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

public class SearchICD10
{
    private readonly ILogger _logger;
    private readonly ICD10SearchService _searchService;

    public SearchICD10(ILoggerFactory loggerFactory, ICD10SearchService searchService)
    {
        _logger = loggerFactory.CreateLogger<SearchICD10>();
        _searchService = searchService;
    }

    [Function("SearchICD10")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<SearchRequest>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Console.WriteLine("\n");
        Console.WriteLine($"******* Searching for ICD-10 codes matching '{input?.Query}' *******");
        Console.WriteLine("\n");

        var icd10Results = await _searchService.SearchICD10Async(input?.Query ?? "");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(icd10Results);
        
        return response;
    }
}

public class SearchRequest
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }
}
