namespace MedicalCodingAssistant.Models;

public class SearchResponse
{
    public bool UsedFreeTextFallback { get; set; }
    public int TotalCount { get; set; }
    public required List<ICD10Result> Results { get; set; }
    public required List<AiICD10Result> AiResults { get; set; }
}
