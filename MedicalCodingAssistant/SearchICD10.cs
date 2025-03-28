using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Utils;
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

        // Validate the codes that the AI service returned against the database, to avoid showing invalid or hallucinated codes
        List<AiICD10Result>? normalizedAiResults = null;
        if (aiResults != null && aiResults.Count > 0)
        {
            // Normalize the AI results to CMS.gov format (E.g., J449 instead of J44.9)
            normalizedAiResults = aiResults.Select(ai => new AiICD10Result
            {
                Code = ICD10CodeNormalizer.ToCMSFormat(ai.Code),
                Description = ai.Description,
                Rank = ai.Rank,
                Reason = ai.Reason
            }).ToList();

            var codeStrings = normalizedAiResults.Select(r => r.Code).Distinct();
            var validCodes = await _searchService.GetValidICD10CodesAsync(codeStrings);

            // If we want to filter out invalid codes
            // aiResults = aiResults
            //     .Where(r => validCodes.Contains(r.Code))
            //     .OrderBy(r => r.Rank)
            //     .ToList();

            // If we want to mark each result as valid or invalid
            foreach (var ai in normalizedAiResults)
            {
                ai.IsValid = validCodes.Contains(ai.Code);
            }
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        var result = new SearchResponse
        {
            UsedFreeTextFallback = searchResults.UsedFreeTextFallback,
            TotalCount = searchResults.TotalCount,
            Results = searchResults.Results,
            AiResults = normalizedAiResults ?? new List<AiICD10Result>()
        };
        
        await response.WriteAsJsonAsync(result);
        return response;
    }
}
