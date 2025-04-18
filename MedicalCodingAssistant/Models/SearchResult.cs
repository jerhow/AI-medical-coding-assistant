namespace MedicalCodingAssistant.Models;

public class SearchResult
{
    public bool UsedFreeTextFallback { get; set; }
    public int TotalSqlOverallMatchCount { get; set; }
    public required List<ICD10Result> DbSearchResults { get; set; }
    public required List<AiICD10Result> SearchResults { get; set; }
}
