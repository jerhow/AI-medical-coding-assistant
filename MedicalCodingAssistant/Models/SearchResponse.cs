namespace MedicalCodingAssistant.Models;

public class SearchResponse
{
    public bool UsedFreeTextFallback { get; set; }
    public int TotalSqlResultCount { get; set; }
    public required List<AiICD10Result> SearchResults { get; set; }
}
