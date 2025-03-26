using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;

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
        var results = await FullTextQueryAsync(query, useContains: true, maxResultsLimited);
        var usedFreeText = false;

        if (results.Count == 0)
        {
            results = await FullTextQueryAsync(query, useContains: false, maxResultsLimited);
            usedFreeText = true;
        }

        return new SearchResponse
        {
            UsedFreeTextFallback = usedFreeText,
            Results = results
        };
    }

    private async Task<List<ICD10Result>> FullTextQueryAsync(string query, bool useContains, int limit)
    {
        var results = new List<ICD10Result>();

        if (string.IsNullOrWhiteSpace(query))
            return results;

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = useContains
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

        using var cmd = new SqlCommand(sql, conn);
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
}

public class ICD10Result
{
    public required string Code { get; set; }
    public required string ShortDescription { get; set; }
    public required string LongDescription { get; set; }
    public required int Rank { get; set; }
}

public class SearchResponse
{
    public bool UsedFreeTextFallback { get; set; }
    public List<ICD10Result> Results { get; set; }
}
