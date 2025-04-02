using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services.Interfaces;
using MedicalCodingAssistant.Utils;

namespace MedicalCodingAssistant.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _deployment;
    private readonly string _apiKey;
    private readonly string _initialPrompt;
    private readonly string _apiVersion;
    private readonly ILogger _logger;

    public OpenAIService(IConfiguration config, ILoggerFactory loggerFactory)
    {
        _httpClient = new HttpClient();
        _endpoint = config["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException("AzureOpenAI:Endpoint configuration is missing.");
        _deployment = config["AzureOpenAI:Deployment"] ?? throw new ArgumentNullException("AzureOpenAI:Deployment configuration is missing.");
        _apiKey = config["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException("AzureOpenAI:ApiKey configuration is missing.");
        _apiVersion = config["AzureOpenAI:ApiVersion"] ?? throw new ArgumentNullException("AzureOpenAI:ApiVersion configuration is missing.");
        _initialPrompt = config["AzureOpenAI:InitialPrompt"] ?? throw new ArgumentNullException("AzureOpenAI:InitialPrompt configuration is missing.");
        _logger = loggerFactory.CreateLogger<SearchICD10>();
    }

    public async Task<AiICD10Response> GetICD10SuggestionsAsync(string diagnosis, List<ICD10Result> sqlResults)
    {
        string url = $"{_endpoint}openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";

        string userMessage = BuildUserMessage(diagnosis, sqlResults);

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = _initialPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.3
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI request failed: {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var jsonResult = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (jsonResult == null)
        {
            throw new Exception("The response from OpenAI did not contain a valid result.");
        }

        AiICD10Response? aiResponse = null;
        try
        {
            aiResponse = JsonSerializer.Deserialize<AiICD10Response>(jsonResult);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get structured AI results for query: {Query}", diagnosis);
        }

        aiResponse.Reranked = await FormatRerankedResultCodes(aiResponse.Reranked);
        
        return aiResponse ?? new AiICD10Response
        {
            Reranked = new List<AiICD10Result>(),
            Additional = new List<AiICD10Result>()
        };
    }

    /// <summary>
    /// Ensure that the re-ranked codes are in the correct format.
    /// When we get the results back from OpenAI, they are sometimes in the non-standard format (e.g., J44.9 instead of J449).
    /// </summary>
    /// <param name="rerankedResults"></param>
    /// <returns></returns>
    private Task<List<AiICD10Result>> FormatRerankedResultCodes(List<AiICD10Result> rerankedResults)
    {
        foreach (var result in rerankedResults)
        {
            result.Code = ICD10CodeNormalizer.ToCMSFormat(result.Code);
        }

        return Task.FromResult(rerankedResults);
    }

    private string BuildUserMessage(string diagnosis, List<ICD10Result> sqlResults)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"The diagnosis is:\n\n\"{diagnosis.Trim()}\"\n");
        sb.AppendLine("The following ICD-10-CM codes were returned by a full-text search:\n");

        string formattedCode = "";
        foreach (var result in sqlResults)
        {
            // Here, a 'formatted' code means it has the decimal point for readability.
            // Trying this format for the codes we are sending to GPT, because it seems to want to return them in this format unless instructed otherwise.
            // It may make no difference, we'll have to observe the results.
            formattedCode = ICD10CodeNormalizer.ToHumanReadableFormat(result.Code);
            sb.AppendLine($"- {formattedCode}: {result.LongDescription}");
        }

        sb.AppendLine("\nPlease re-rank these codes based on relevance to the diagnosis, and suggest any additional ICD-10-CM codes that might be more appropriate or are missing.");

        return sb.ToString();
    }

}
