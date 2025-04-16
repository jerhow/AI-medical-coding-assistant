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
    private readonly IConfiguration _config;

    public SearchICD10(ILoggerFactory loggerFactory, IICD10SearchService searchService, IConfiguration configuration, IOpenAIService aiService)
    {
        _logger = loggerFactory.CreateLogger<SearchICD10>();
        _searchService = searchService;
        _defaultMaxResults = configuration.GetValue<int>("DefaultMaxResults");
        _aiService = aiService;
        _config = configuration;
    }

    [Function("SearchICD10")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SearchICD10")] HttpRequestData req)
    {
        if (!ValidateApiKey(req))
        {
            _logger.LogWarning("Unauthorized request: missing or invalid API key");
            return req.CreateResponse(HttpStatusCode.Forbidden); // 403 Forbidden
        }

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
        int maxResults = 0;
        if (input?.MaxSqlResults == null || input.MaxSqlResults < 1)
        {
            maxResults = _defaultMaxResults;
        }
        else
        {
            maxResults = input.MaxSqlResults;
        }

        // Make sure the query itself (the natural language diagnosis or clinical description) is not empty
        if (string.IsNullOrWhiteSpace(query))
        {
            HttpResponseData emptyResponse = req.CreateResponse(HttpStatusCode.BadRequest); // 400
            await emptyResponse.WriteStringAsync("Query cannot be empty.");
            return emptyResponse;
        }

        // Fetch the code suggestions from the database ("search") and AI service ("AI")
        SearchResult searchResult = await _searchService.SearchICD10Async(query, maxResults);
        List<AiICD10Result>? aiResponse = await _aiService.GetICD10SuggestionsAsync(query, searchResult.DbSearchResults);
        aiResponse = ICD10CodeNormalizer.FormatCodes(aiResponse, "CMS"); // Codes must be in CMS format for internal consistency

        // Validate the AI results against the database to ensure that the codes are valid and not hallucinated
        List<AiICD10Result> normalizedAiResponse = aiResponse != null 
            ? await ValidateAICodes(aiResponse) 
            : new List<AiICD10Result>();
        
        // Construct the result payload with the DB search and the normalized AI results, and return it as a JSON response
        var response = req.CreateResponse(HttpStatusCode.OK);
        var result = new SearchResponse
        {
            UsedFreeTextFallback = searchResult.UsedFreeTextFallback,
            TotalSqlResultCount = searchResult.TotalSqlResultCount,
            AiModel = _config["AzureOpenAI:Deployment"] ?? string.Empty,
            AiVersion = _config["AzureOpenAI:ApiVersion"] ?? string.Empty,
            AiTemperature = double.TryParse(_config["AzureOpenAI:Temperature"], out var temperature) ? temperature : 0.3,
            SearchResults = ICD10CodeNormalizer.FormatCodes(normalizedAiResponse, "HumanReadable")
        };
        
        await response.WriteAsJsonAsync(result);
        return response;
    }

    /// <summary>
    /// Validates the AI results against the database to ensure that the codes are valid and not hallucinated.
    /// This method expects the codes in 'aiResponse' to be in CMS format (e.g., J449 instead of J44.9).
    /// </summary>
    /// <param name="aiResponse"></param>
    /// <returns></returns>
    private async Task<List<AiICD10Result>> ValidateAICodes(List<AiICD10Result> aiResponse)
    {
        if (aiResponse == null || aiResponse.Count == 0)
        {
            return new List<AiICD10Result>();
        }
        
        var codeStrings = aiResponse.Select(r => r.Code).Distinct(); // Collect the codes from the AI response
        var validCodes = await _searchService.GetValidICD10CodesAsync(codeStrings); // Only the codes from `codeStrings` that exist in the ICD-10 database (i.e., are valid)

        // Mark each result as valid or invalid
        foreach (var ai in aiResponse)
        {
            ai.IsValid = validCodes.Contains(ai.Code);
        }

        return aiResponse;
    }

    /// <summary>
    /// Validates the API key from the request headers.
    /// The API key is expected to be in the "Authorization" header with the format "Bearer {key}".
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    private bool ValidateApiKey(HttpRequestData req)
    {
        const string prefix = "Bearer ";

        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            return false;
        }

        var authHeader = authHeaders.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith(prefix))
        {
            return false;
        }

        var providedKey = authHeader.Substring(prefix.Length).Trim();
        var expectedKey = _config["ApiKey"];

        return !string.IsNullOrWhiteSpace(expectedKey) && providedKey == expectedKey;
    }

}
