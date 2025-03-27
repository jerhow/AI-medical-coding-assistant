using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

public class OpenAIService
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
        _endpoint = config["AzureOpenAI:Endpoint"];
        _deployment = config["AzureOpenAI:Deployment"];
        _apiKey = config["AzureOpenAI:ApiKey"];
        _apiVersion = config["AzureOpenAI:ApiVersion"];
        _initialPrompt = config["AzureOpenAI:InitialPrompt"];
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

        return result;
    }
}
