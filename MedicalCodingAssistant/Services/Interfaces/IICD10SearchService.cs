using MedicalCodingAssistant.Models;

namespace MedicalCodingAssistant.Services.Interfaces;

public interface IICD10SearchService
{
    Task<SearchResponse> SearchICD10Async(string query, int maxResults);
    Task<(List<ICD10Result>, int TotalCount)> FullTextQueryAsync(string query, bool useContains, int limit);
    Task<List<ICD10Result>> GetResultsAsync(string query, bool useContains, int limit);
    Task<int> GetTotalCountAsync(string query, bool useContains);
}
