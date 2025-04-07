using MedicalCodingAssistant.Models;

namespace MedicalCodingAssistant.Utils;

public static class ICD10CodeNormalizer
{
    /// <summary>
    /// Takes the response from the AI service call and formats the codes.
    /// The only options are CMS (official) format (no decimal point) or human-readable format (with the decimal point).
    /// </summary>
    /// <param name="aiResponse"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static List<AiICD10Result> FormatCodes(List<AiICD10Result> aiResponse, string format)
    {
        if (aiResponse == null || aiResponse.Count == 0)
        {
            return new List<AiICD10Result>();
        }

        format = format.ToUpperInvariant();

        foreach (var ai in aiResponse)
        {
            if (format == "CMS")
            {
                ai.Code = ICD10CodeNormalizer.ToCMSFormat(ai.Code); // Convert the code to CMS format (e.g., J449 instead of J44.9)
            }
            else
            {
                ai.Code = ICD10CodeNormalizer.ToHumanReadableFormat(ai.Code); // Convert the code to a human-readable format (e.g., J44.9 instead of J449)
            }
        }

        return aiResponse;
    }

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

        if (code.Contains("."))
        {
            return code;
        }

        // Insert a dot after the third character if the code length is greater than 3
        if (code.Length > 3)
        {
            code = code.Insert(3, ".");
        }

        return code;
    }
}
