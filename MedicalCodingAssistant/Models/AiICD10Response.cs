using System.Text.Json.Serialization;

namespace MedicalCodingAssistant.Models;
public class AiICD10Response
{
    [JsonPropertyName("reranked")]
    public List<AiICD10Result> Reranked { get; set; } = new();

    [JsonPropertyName("additional")]
    public List<AiICD10Result> Additional { get; set; } = new();
}
