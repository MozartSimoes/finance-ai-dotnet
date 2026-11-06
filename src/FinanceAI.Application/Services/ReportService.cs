using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Domain.Entities;
using FinanceAI.Domain.Exceptions;

namespace FinanceAI.Application.Services;

public interface IReportService
{
    Task<MonthlySummaryResponse> GetMonthlySummaryAsync(int year, int month);
}

public class ReportService(ITransactionRepository transactions) : IReportService
{
    public async Task<MonthlySummaryResponse> GetMonthlySummaryAsync(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new ValidationException("Ano ou mês inválido.");

        var from = new DateTime(year, month, 1);
        var totals = await transactions.GetTotalsByCategoryAsync(from, from.AddMonths(1).AddDays(-1));

        var income = totals.Where(t => t.Type == TransactionType.Income).Sum(t => t.Total);
        var expense = totals.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Total);

        return new MonthlySummaryResponse(
            year, month, income, expense, income - expense,
            totals.OrderByDescending(t => t.Total).ToList());
    }
}
