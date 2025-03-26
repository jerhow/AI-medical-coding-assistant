using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;

public class ICD10SearchService
{
    private readonly string? _connectionString;

    public ICD10SearchService(IConfiguration configuration)
    {
        _connectionString = configuration["SqlConnectionString"];
    }

    public async Task<List<ICD10Result>> SearchICD10Async(string query)
    {
        var results = new List<ICD10Result>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(@"
            SELECT TOP 10 Code, short_desc, long_desc
            FROM dbo.cms_icd10_valid
            WHERE CONTAINS(long_desc, @query);
        ", conn);

        cmd.Parameters.AddWithValue("@query", $"\"{query}\"");

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ICD10Result
            {
                Code = reader.GetString(0),
                ShortDescription = reader.GetString(1),
                LongDescription = reader.GetString(2)
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
}
