namespace MedicalCodingAssistant.Utils;

public static class ICD10CodeNormalizer
{
    /// <summary>
    /// Official CMD format for ICD-10-CM codes is without a decimal point.
    /// For example, J44.9 is formatted as J449.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static string ToCMSFormat(string code)
    {
        return code?.Replace(".", "", StringComparison.OrdinalIgnoreCase).Trim().ToUpperInvariant() ?? "";
    }

    /// <summary>
    /// To format ICD-10-CM codes for readability, a decimal point is placed after the first three characters.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static string ToHumanReadableFormat(string code)
    {
        // Make sure the code is not null or empty
        if (string.IsNullOrEmpty(code))
        {
            return "";
        }

        code = code.Trim().ToUpperInvariant();

        // Insert a dot after the third character if the code length is greater than 3
        if (code.Length > 3)
        {
            code = code.Insert(3, ".");
        }

        return code;
    }
}
