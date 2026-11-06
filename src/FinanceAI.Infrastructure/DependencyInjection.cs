using Dapper;
using FinanceAI.Application.Abstractions;
using FinanceAI.Infrastructure.Persistence;
using FinanceAI.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FinanceAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

        // snake_case columns (category_id) map to PascalCase properties (CategoryId).
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        return services;
    }
}
