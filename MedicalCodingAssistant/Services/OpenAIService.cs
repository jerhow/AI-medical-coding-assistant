using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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

    public async Task<string> GetICD10SuggestionAsync(string diagnosis)
    {
        var url = $"{_endpoint}openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = _initialPrompt },
                new { role = "user", content = $"Suggest ICD-10 codes for: {diagnosis}" }
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
}
