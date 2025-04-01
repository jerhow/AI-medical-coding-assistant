using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services.Interfaces;

namespace MedicalCodingAssistant.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _deployment;
    private readonly string _apiKey;
    private readonly string _initialPrompt;
    private readonly string _apiVersion;

    public OpenAIService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _endpoint = config["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException("AzureOpenAI:Endpoint configuration is missing.");
        _deployment = config["AzureOpenAI:Deployment"] ?? throw new ArgumentNullException("AzureOpenAI:Deployment configuration is missing.");
        _apiKey = config["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException("AzureOpenAI:ApiKey configuration is missing.");
        _apiVersion = config["AzureOpenAI:ApiVersion"] ?? throw new ArgumentNullException("AzureOpenAI:ApiVersion configuration is missing.");
        _initialPrompt = config["AzureOpenAI:InitialPrompt"] ?? throw new ArgumentNullException("AzureOpenAI:InitialPrompt configuration is missing.");
    }

    public async Task<string> GetICD10SuggestionsAsync(string diagnosis, List<ICD10Result> sqlResults)
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
        var result = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (result == null)
        {
            throw new Exception("The response from OpenAI did not contain a valid result.");
        }

        return result;
    }

    private string BuildUserMessage(string diagnosis, List<ICD10Result> sqlResults)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"The diagnosis is:\n\n\"{diagnosis.Trim()}\"\n");
        sb.AppendLine("The following ICD-10-CM codes were returned by a full-text search:\n");

        foreach (var result in sqlResults)
        {
            // If you want to display with a dot, you can re-format: e.g., J449 => J44.9
            sb.AppendLine($"- {result.Code}: {result.LongDescription}");
        }

        sb.AppendLine("\nPlease re-rank these codes based on relevance to the diagnosis, and suggest any additional ICD-10-CM codes that might be more appropriate or are missing.");

        return sb.ToString();
    }

}
