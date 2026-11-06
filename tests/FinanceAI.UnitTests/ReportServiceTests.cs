using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Application.Services;
using FinanceAI.Domain.Entities;
using FinanceAI.Domain.Exceptions;
using Moq;

namespace FinanceAI.UnitTests;

public class ReportServiceTests
{
    [Fact]
    public async Task MonthlySummary_ComputesTotalsAndBalance()
    {
        var repository = new Mock<ITransactionRepository>();
        repository.Setup(r => r.GetTotalsByCategoryAsync(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)))
            .ReturnsAsync([
                new CategoryTotal("Salário", TransactionType.Income, 10_000m, 1),
                new CategoryTotal("Moradia", TransactionType.Expense, 3_000m, 3),
                new CategoryTotal("Alimentação", TransactionType.Expense, 1_500m, 12)
            ]);

        var summary = await new ReportService(repository.Object).GetMonthlySummaryAsync(2026, 2);

        Assert.Equal(10_000m, summary.TotalIncome);
        Assert.Equal(4_500m, summary.TotalExpense);
        Assert.Equal(5_500m, summary.Balance);
        Assert.Equal("Salário", summary.ByCategory[0].Category);
    }

    [Theory]
    [InlineData(2026, 13)]
    [InlineData(1999, 1)]
    public async Task MonthlySummary_InvalidPeriod_ThrowsValidation(int year, int month)
    {
        var service = new ReportService(Mock.Of<ITransactionRepository>());
        await Assert.ThrowsAsync<ValidationException>(() => service.GetMonthlySummaryAsync(year, month));
    }
}
