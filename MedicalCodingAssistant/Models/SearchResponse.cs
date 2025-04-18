namespace MedicalCodingAssistant.Models;

public class SearchResponse
{
    public bool UsedFreeTextFallback { get; set; }
    public int TotalSqlOverallMatchCount { get; set; }
    public string AiModel { get; set; } = string.Empty;
    public string AiVersion { get; set; } = string.Empty;
    public double AiTemperature { get; set; }
    public required List<AiICD10Result> SearchResults { get; set; }
}
