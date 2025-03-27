namespace MedicalCodingAssistant.Services.Interfaces;

public interface IOpenAIService
{
    Task<string> GetICD10SuggestionAsync(string diagnosis);
}
