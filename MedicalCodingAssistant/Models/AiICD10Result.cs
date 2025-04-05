using System.Text.Json.Serialization;

namespace MedicalCodingAssistant.Models;

public class AiICD10Result
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }
    
    [JsonPropertyName("description")]
    public required string Description { get; set; }
    
    [JsonPropertyName("rank")]
    public int Rank { get; set; } = 0;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("confidence")]
    public int Confidence { get; set; } = 0;
    
    public bool IsValid { get; set; } = false; // Default to false until validation takes place
}