using MedicalCodingAssistant.Models;

namespace MedicalCodingAssistant.Services.Interfaces;

public interface IOpenAIService
{
    Task<string> GetICD10SuggestionsAsync(string diagnosis, List<ICD10Result> sqlResults);
}
