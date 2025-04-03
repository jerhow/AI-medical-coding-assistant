using MedicalCodingAssistant.Models;

namespace MedicalCodingAssistant.Services.Interfaces;

public interface IOpenAIService
{
    Task<List<AiICD10Result>> GetICD10SuggestionsAsync(string diagnosis, List<ICD10Result> sqlResults);
}
