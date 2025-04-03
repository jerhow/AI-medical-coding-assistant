using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
    private readonly double _apiTemperature;
    private readonly string _apiUserMessageAdditionalContext;
    private readonly ILogger _logger;
    private readonly GptLoggingService _gptLoggingService;

    public OpenAIService(IConfiguration config, ILoggerFactory loggerFactory, GptLoggingService gptLoggingService)
    {
        _httpClient = new HttpClient();
        _endpoint = config["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException("AzureOpenAI:Endpoint configuration is missing.");
        _deployment = config["AzureOpenAI:Deployment"] ?? throw new ArgumentNullException("AzureOpenAI:Deployment configuration is missing.");
        _apiKey = config["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException("AzureOpenAI:ApiKey configuration is missing.");
        _apiVersion = config["AzureOpenAI:ApiVersion"] ?? throw new ArgumentNullException("AzureOpenAI:ApiVersion configuration is missing.");
        _initialPrompt = config["AzureOpenAI:InitialPrompt"] ?? throw new ArgumentNullException("AzureOpenAI:InitialPrompt configuration is missing.");
        _apiTemperature = config.GetValue<double>("AzureOpenAI:Temperature", 0.3);
        _apiUserMessageAdditionalContext = config["AzureOpenAI:UserMessageAdditionalContext"] ?? throw new ArgumentNullException("AzureOpenAI:UserMessageAdditionalContext configuration is missing.");
        _logger = loggerFactory.CreateLogger<SearchICD10>();
        _gptLoggingService = gptLoggingService;
    }

    public async Task<List<AiICD10Result>> GetICD10SuggestionsAsync(string diagnosis, List<ICD10Result> sqlResults)
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
            temperature = _apiTemperature
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var stopwatch = Stopwatch.StartNew(); // Start the stopwatch to measure response time
        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        stopwatch.Stop();

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

        List<AiICD10Result>? aiResult = null;
        try
        {
            aiResult = JsonSerializer.Deserialize<List<AiICD10Result>>(jsonResult);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get structured AI results for query: {Query}", diagnosis);
        }

        _gptLoggingService.Log(new GptResponseLog
        {
            ApiVersion = _apiVersion,
            Query = diagnosis,
            SqlResultCount = sqlResults.Count,
            SqlResults = sqlResults,
            GptResponseJson = jsonResult,
            Temperature = _apiTemperature,
            ResponseTime = stopwatch.Elapsed,
            SystemPrompt = _initialPrompt,
            UserPrompt = userMessage,
            DeploymentName = _deployment,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });

        return aiResult ?? new List<AiICD10Result>();
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

        sb.AppendLine(_apiUserMessageAdditionalContext);

        return sb.ToString();
    }

}
