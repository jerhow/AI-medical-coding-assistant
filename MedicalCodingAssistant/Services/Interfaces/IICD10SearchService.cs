using MedicalCodingAssistant.Models;

namespace MedicalCodingAssistant.Services.Interfaces;

public interface IICD10SearchService
{
    Task<SearchResult> SearchICD10Async(string query, int maxResults);
    Task<(List<ICD10Result>, int TotalCount)> FullTextQueryAsync(string query, bool useContains, int limit);
    Task<List<ICD10Result>> GetResultsAsync(string query, bool useContains, int limit);
    Task<int> GetTotalCountAsync(string query, bool useContains);
    Task<HashSet<string>> GetValidICD10CodesAsync(IEnumerable<string> codes);
}
