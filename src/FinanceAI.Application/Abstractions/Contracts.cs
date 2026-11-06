using FinanceAI.Application.Dtos;
using FinanceAI.Domain.Entities;

namespace FinanceAI.Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category?> GetByNameAsync(string name);
    Task<int> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(int id);
    Task<bool> HasTransactionsAsync(int id);
}

/// <summary>
/// Writes take entities; reads return response models straight from SQL (Dapper),
/// avoiding an extra mapping step for joined data.
/// </summary>
public interface ITransactionRepository
{
    Task<TransactionResponse?> GetByIdAsync(long id);
    Task<PagedResult<TransactionResponse>> SearchAsync(TransactionFilter filter);
    Task<long> CreateAsync(Transaction transaction);
    Task<bool> UpdateAsync(Transaction transaction);
    Task<bool> DeleteAsync(long id);
    Task<IReadOnlyList<CategoryTotal>> GetTotalsByCategoryAsync(DateTime from, DateTime to);
}

public interface ITransactionClassifier
{
    ClassificationResult Classify(string description);
}
