using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

public class SearchICD10
{
    private readonly ILogger _logger;
    private readonly ICD10SearchService _searchService;
    private readonly int _defaultMaxResults;

    public SearchICD10(ILoggerFactory loggerFactory, ICD10SearchService searchService, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<SearchICD10>();
        _searchService = searchService;
        _defaultMaxResults = configuration.GetValue<int>("DefaultMaxResults");
    }

    [Function("SearchICD10")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        SearchRequest input;

        try {
            input = JsonSerializer.Deserialize<SearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new SearchRequest();
        }
        catch (JsonException)
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest); // 400
            await badRequestResponse.WriteStringAsync("Invalid JSON request body");
            return badRequestResponse;
        }

        var query = input?.Query?.Trim();
        var maxResults = input?.MaxResults ?? _defaultMaxResults;

        if (string.IsNullOrWhiteSpace(query))
        {
            var emptyResponse = req.CreateResponse(HttpStatusCode.BadRequest); // 400
            await emptyResponse.WriteStringAsync("Query cannot be empty.");
            return emptyResponse;
        }

        var icd10Results = await _searchService.SearchICD10Async(query, maxResults);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(icd10Results);
        
        return response;
    }
}

public class SearchRequest
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; } = 0;
}
