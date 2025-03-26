namespace MedicalCodingAssistant.Models
{
    public class SearchResponse
    {
        public bool UsedFreeTextFallback { get; set; }
        public required List<ICD10Result> Results { get; set; }
    }
}
