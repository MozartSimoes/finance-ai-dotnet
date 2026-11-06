using FinanceAI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
