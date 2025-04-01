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

        // Deserialize the request body into the SearchRequest object
        // If deserialization fails, return a 400 Bad Request response
        try {
            input = JsonSerializer.Deserialize<SearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new SearchRequest();
        }
        catch (JsonException)
        {
            HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest); // 400
            await badRequestResponse.WriteStringAsync("Invalid JSON request body");
            return badRequestResponse;
        }

        // Extract the query from the input object and set a default max results value
        var query = input?.Query?.Trim();
        var maxResults = input?.MaxResults ?? _defaultMaxResults;

        // Make sure the query itself (the natural language diagnosis or clinical description) is not empty
        if (string.IsNullOrWhiteSpace(query))
        {
            HttpResponseData emptyResponse = req.CreateResponse(HttpStatusCode.BadRequest); // 400
            await emptyResponse.WriteStringAsync("Query cannot be empty.");
            return emptyResponse;
        }

        // Fetch the code suggestions from the database ("search") and AI service ("AI")
        SearchResponse searchResponse = await _searchService.SearchICD10Async(query, maxResults);
        AiICD10Response? aiResponse = null;
        aiResponse = await _aiService.GetICD10SuggestionsAsync(query, searchResponse.SearchResults);

        // Validate the AI results against the database to ensure that the codes are valid and not hallucinated
        List<AiICD10Result> normalizedAiResults = aiResponse?.Additional != null 
            ? await ValidateAICodes(aiResponse.Additional) 
            : new List<AiICD10Result>();

        // Construct the result payload with the DB search and the normalized AI results, and return it as a JSON response
        var response = req.CreateResponse(HttpStatusCode.OK);
        var result = new SearchResponse
        {
            UsedFreeTextFallback = searchResponse.UsedFreeTextFallback,
            TotalCount = searchResponse.TotalCount,
            SearchResults = searchResponse.SearchResults,
            SearchResultsReranked = normalizedAiResults,
            AiAddtionalResults = aiResponse.Additional
        };
        
        await response.WriteAsJsonAsync(result);
        return response;
    }

    /// <summary>
    /// Validates the AI results against the database to ensure that the codes are valid and not hallucinated.
    /// </summary>
    /// <param name="aiResults"></param>
    /// <returns></returns>
    private async Task<List<AiICD10Result>> ValidateAICodes(List<AiICD10Result> aiResults)
    {
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

        return normalizedAiResults ?? new List<AiICD10Result>();
    }
}
