using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services;
using MedicalCodingAssistant.Services.Interfaces;

public class SearchICD10
{
    private readonly ILogger _logger;
    private readonly IICD10SearchService _searchService;
    private readonly int _defaultMaxResults;
    private readonly IOpenAIService _aiService;

    public SearchICD10(ILoggerFactory loggerFactory, IICD10SearchService searchService, IConfiguration configuration, IOpenAIService aiService)
    {
        _logger = loggerFactory.CreateLogger<SearchICD10>();
        _searchService = searchService;
        _defaultMaxResults = configuration.GetValue<int>("DefaultMaxResults");
        _aiService = aiService;
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

        var searchResults = await _searchService.SearchICD10Async(query, maxResults);
        List<AiICD10Result>? aiResults = null;
        try
        {
            var aiResponse = await _aiService.GetICD10SuggestionAsync(query);
            aiResults = JsonSerializer.Deserialize<List<AiICD10Result>>(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get structured AI results for query: {Query}", query);
        }
                
        var response = req.CreateResponse(HttpStatusCode.OK);
        var result = new SearchResponse
        {
            UsedFreeTextFallback = searchResults.UsedFreeTextFallback,
            TotalCount = searchResults.TotalCount,
            Results = searchResults.Results,
            AiResults = aiResults ?? new List<AiICD10Result>()
        };
        
        await response.WriteAsJsonAsync(result);
        return response;
    }
}
