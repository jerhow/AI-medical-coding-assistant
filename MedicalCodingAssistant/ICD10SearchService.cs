using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using MedicalCodingAssistant.Models;

public class ICD10SearchService
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
            Results = results
        };
    }

    private async Task<(List<ICD10Result>, int TotalCount)> FullTextQueryAsync(string query, bool useContains, int limit)
    {
        var results = new List<ICD10Result>();

        if (string.IsNullOrWhiteSpace(query))
            return (results, 0);

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
                
        using (var resultCmd = new SqlCommand(resultSql, conn))
        {
            resultCmd.Parameters.AddWithValue("@query", useContains ? $"\"{query}\"" : query);
            resultCmd.Parameters.AddWithValue("@limit", limit);

            using var reader = await resultCmd.ExecuteReaderAsync();
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
        }

        var countSql = useContains
            ? "SELECT COUNT(*) FROM dbo.cms_icd10_valid WHERE CONTAINS(long_desc, @query)"
            : "SELECT COUNT(*) FROM dbo.cms_icd10_valid WHERE FREETEXT(long_desc, @query)";
        
        var totalCount = 0;
        using (var countCmd = new SqlCommand(countSql, conn))
        {
            countCmd.Parameters.AddWithValue("@query", useContains ? $"\"{query}\"" : query);
            var scalarResult = await countCmd.ExecuteScalarAsync();
            totalCount = scalarResult != null ? (int)scalarResult : 0;
        } 

        return (results, totalCount);
    }
}




