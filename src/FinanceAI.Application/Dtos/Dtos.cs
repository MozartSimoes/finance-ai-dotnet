using FinanceAI.Domain.Entities;

namespace FinanceAI.Application.Dtos;

public record CategoryRequest(string Name, TransactionType Type);

public record CategoryResponse(int Id, string Name, TransactionType Type);

/// <param name="CategoryId">Optional: when omitted, the ML model classifies the transaction.</param>
public record TransactionRequest(string Description, decimal Amount, DateTime Date, int? CategoryId);

public record TransactionResponse(
    long Id,
    string Description,
    decimal Amount,
    DateTime Date,
    TransactionType Type,
    int CategoryId,
    string CategoryName,
    bool CategorySuggestedByAi);

public record TransactionFilter(DateTime? From, DateTime? To, int? CategoryId, int Page = 1, int PageSize = 20);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems);

public record ClassificationResult(string Category, float Confidence);

public record CategoryTotal(string Category, TransactionType Type, decimal Total, int Count);

public record MonthlySummaryResponse(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    IReadOnlyList<CategoryTotal> ByCategory);
