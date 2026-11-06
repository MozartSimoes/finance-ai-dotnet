using Dapper;
using Npgsql;

namespace FinanceAI.Infrastructure.Persistence;

public class DatabaseInitializer(NpgsqlDataSource dataSource)
{
    public async Task InitializeAsync()
    {
        await using var stream = typeof(DatabaseInitializer).Assembly
            .GetManifestResourceStream("FinanceAI.Infrastructure.Persistence.schema.sql")
            ?? throw new InvalidOperationException("schema.sql não encontrado como recurso embutido.");
        using var reader = new StreamReader(stream);
        var script = await reader.ReadToEndAsync();

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(script);
    }
}
