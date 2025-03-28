namespace MedicalCodingAssistant.Utils;

public static class ICD10CodeNormalizer
{
    public static string ToCMSFormat(string code)
    {
        return code?.Replace(".", "", StringComparison.OrdinalIgnoreCase).Trim().ToUpperInvariant() ?? "";
    }
}
