using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using MedicalCodingAssistant.Models;
using MedicalCodingAssistant.Services.Interfaces;

namespace MedicalCodingAssistant.Services;

public class ICD10SearchService : IICD10SearchService
{
    private readonly string? _connectionString;
    private readonly int _maxAllowedResults;

    public ICD10SearchService(IConfiguration configuration)
    {
        _connectionString = configuration["SqlConnectionString"];
        _maxAllowedResults = configuration.GetValue<int>("MaxAllowedResults");
    }

    public async Task<SearchResponse> SearchICD10Async(string query, int maxResults)
    {
        var maxResultsLimited = Math.Clamp(maxResults, 1, _maxAllowedResults);
        var (results, totalCount) = await FullTextQueryAsync(query, useContains: true, maxResultsLimited);
        var usedFreeText = false;

        if (results.Count == 0)
        {
            (results, totalCount) = await FullTextQueryAsync(query, useContains: false, maxResultsLimited);
            usedFreeText = true;
        }

        return new SearchResponse
        {
            UsedFreeTextFallback = usedFreeText,
            TotalCount = totalCount,
            SearchResults = results,
            SearchResultsReranked = new List<AiICD10Result>(), // Initialize with an empty list since we don't have this yet
            AiAddtionalResults = new List<AiICD10Result>() // Initialize with an empty list since we don't have this yet
        };
    }

    public async Task<(List<ICD10Result>, int TotalCount)> FullTextQueryAsync(string query, bool useContains, int limit)
    {
        var results = new List<ICD10Result>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return (results, 0);
        }

        results = await GetResultsAsync(query, useContains, limit);
        var totalCount = await GetTotalCountAsync(query, useContains);
        return (results, totalCount);
    }

    public async Task<List<ICD10Result>> GetResultsAsync(string query, bool useContains, int limit)
    {
        var results = new List<ICD10Result>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var resultSql = useContains
            ? @"SELECT TOP (@limit) code, long_desc, FTT.RANK
                FROM dbo.cms_icd10_valid AS ICD
                INNER JOIN CONTAINSTABLE(dbo.cms_icd10_valid, long_desc, @query) AS FTT
                    ON ICD.ID = FTT.[KEY]
                ORDER BY FTT.RANK DESC, ICD.Code ASC;"
            : @"SELECT TOP (@limit) code, long_desc, FTT.RANK
                FROM dbo.cms_icd10_valid AS ICD
                INNER JOIN FREETEXTTABLE(dbo.cms_icd10_valid, long_desc, @query) AS FTT
                    ON ICD.ID = FTT.[KEY]
                ORDER BY FTT.RANK DESC, ICD.Code ASC;";

        using var cmd = new SqlCommand(resultSql, conn);
        cmd.Parameters.AddWithValue("@query", useContains ? $"\"{query}\"" : query);
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ICD10Result
            {
                Code = reader.GetString(0),
                LongDescription = reader.GetString(1),
                Rank = reader.GetInt32(2)
            });
        }

        return results;
    }

    public async Task<int> GetTotalCountAsync(string query, bool useContains)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var countSql = useContains
            ? "SELECT COUNT(*) FROM dbo.cms_icd10_valid WHERE CONTAINS(long_desc, @query)"
            : "SELECT COUNT(*) FROM dbo.cms_icd10_valid WHERE FREETEXT(long_desc, @query)";

        using var cmd = new SqlCommand(countSql, conn);
        cmd.Parameters.AddWithValue("@query", useContains ? $"\"{query}\"" : query);

        var scalarResult = await cmd.ExecuteScalarAsync();
        var count = scalarResult != null ? (int)scalarResult : 0;
        return count;
    }

    /// <summary>
    /// Take the list of codes from the AI service call, and match them against the codes in the database,
    /// returning a list of only valid codes.
    /// The caller of this method uses this to filter out invalid or hallucinated codes from the AI service.
    /// </summary>
    /// <param name="codes"></param>
    /// <returns></returns>
    public async Task<HashSet<string>> GetValidICD10CodesAsync(IEnumerable<string> codes)
    {
        var validCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!codes.Any()) 
        {
            return validCodes;
        }

        var codeList = codes.ToList();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var parameters = codeList.Select((code, i) => $"@code{i}").ToList();
        var query = $"SELECT Code FROM dbo.cms_icd10cm_2025 WHERE code IN ({string.Join(", ", parameters)})";

        using var cmd = new SqlCommand(query, conn);
        for (int i = 0; i < codeList.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@code{i}", codeList[i]);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            validCodes.Add(reader.GetString(0));
        }

        return validCodes;
    }
}
