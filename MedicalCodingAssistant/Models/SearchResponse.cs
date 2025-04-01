namespace MedicalCodingAssistant.Models;

public class SearchResponse
{
    public bool UsedFreeTextFallback { get; set; }
    public int TotalCount { get; set; }
    public required List<ICD10Result> SearchResults { get; set; }
    public required List<AiICD10Result> SearchResultsReranked { get; set; }
    public required List<AiICD10Result> AiAddtionalResults { get; set; }
}
