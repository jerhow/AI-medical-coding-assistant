using System.Text.Json.Serialization;

namespace MedicalCodingAssistant.Models;

public class SearchRequest
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; } = 0;
}
