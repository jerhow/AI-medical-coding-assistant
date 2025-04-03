using System.Text.Json.Serialization;

namespace MedicalCodingAssistant.Models;

public class SearchRequest
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("maxSqlResults")]
    public int MaxSqlResults { get; set; } = 0;
}
