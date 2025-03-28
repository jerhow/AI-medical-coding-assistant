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
            Results = results,
            AiResults = new List<AiICD10Result>() // Initialize with an empty list since we don't have AI results yet
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
            ? @"SELECT TOP (@limit) Code, short_desc, long_desc, FTT.RANK
                FROM dbo.cms_icd10_valid AS ICD
                INNER JOIN CONTAINSTABLE(dbo.cms_icd10_valid, long_desc, @query) AS FTT
                    ON ICD.ID = FTT.[KEY]
                ORDER BY FTT.RANK DESC, ICD.Code ASC;"
            : @"SELECT TOP (@limit) Code, short_desc, long_desc, FTT.RANK
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
                ShortDescription = reader.GetString(1),
                LongDescription = reader.GetString(2),
                Rank = reader.GetInt32(3)
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
}
