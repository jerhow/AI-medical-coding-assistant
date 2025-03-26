namespace MedicalCodingAssistant.Models
{
    public class ICD10Result
    {
        public required string Code { get; set; }
        public required string ShortDescription { get; set; }
        public required string LongDescription { get; set; }
        public required int Rank { get; set; }
    }
}
