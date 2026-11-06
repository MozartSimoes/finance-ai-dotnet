using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Domain.Entities;
using FinanceAI.Domain.Exceptions;

namespace FinanceAI.Application.Services;

public interface ITransactionService
{
    Task<TransactionResponse> GetByIdAsync(long id);
    Task<PagedResult<TransactionResponse>> SearchAsync(TransactionFilter filter);
    Task<TransactionResponse> CreateAsync(TransactionRequest request);
    Task<TransactionResponse> UpdateAsync(long id, TransactionRequest request);
    Task DeleteAsync(long id);
    ClassificationResult Classify(string description);
}

public class TransactionService(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    ITransactionClassifier classifier) : ITransactionService
{
    /// <summary>Below this confidence the prediction is discarded and the fallback category is used.</summary>
    public const float MinimumConfidence = 0.5f;
    public const string FallbackCategory = "Outros";

    public async Task<TransactionResponse> GetByIdAsync(long id) =>
        await transactions.GetByIdAsync(id) ?? throw new NotFoundException("Lançamento", id);

    public Task<PagedResult<TransactionResponse>> SearchAsync(TransactionFilter filter)
    {
        if (filter.From > filter.To)
            throw new ValidationException("A data inicial deve ser anterior à data final.");

        var normalized = filter with
        {
            Page = Math.Max(filter.Page, 1),
            PageSize = Math.Clamp(filter.PageSize, 1, 100)
        };
        return transactions.SearchAsync(normalized);
    }

    public async Task<TransactionResponse> CreateAsync(TransactionRequest request)
    {
        Validate(request);
        var (category, suggestedByAi) = await ResolveCategoryAsync(request);

        var transaction = new Transaction
        {
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Date = request.Date.Date,
            CategoryId = category.Id,
            CategorySuggestedByAi = suggestedByAi
        };
        var id = await transactions.CreateAsync(transaction);
        return await GetByIdAsync(id);
    }

    public async Task<TransactionResponse> UpdateAsync(long id, TransactionRequest request)
    {
        Validate(request);
        await GetByIdAsync(id);
        var (category, suggestedByAi) = await ResolveCategoryAsync(request);

        await transactions.UpdateAsync(new Transaction
        {
            Id = id,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Date = request.Date.Date,
            CategoryId = category.Id,
            CategorySuggestedByAi = suggestedByAi
        });
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(long id)
    {
        if (!await transactions.DeleteAsync(id))
            throw new NotFoundException("Lançamento", id);
    }

    public ClassificationResult Classify(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ValidationException("Informe a descrição a ser classificada.");
        return classifier.Classify(description.Trim());
    }

    private async Task<(Category Category, bool SuggestedByAi)> ResolveCategoryAsync(TransactionRequest request)
    {
        if (request.CategoryId is int categoryId)
        {
            var chosen = await categories.GetByIdAsync(categoryId)
                ?? throw new NotFoundException("Categoria", categoryId);
            return (chosen, false);
        }

        var prediction = classifier.Classify(request.Description.Trim());
        var name = prediction.Confidence >= MinimumConfidence ? prediction.Category : FallbackCategory;

        var predicted = await categories.GetByNameAsync(name)
            ?? await categories.GetByNameAsync(FallbackCategory)
            ?? throw new NotFoundException("Categoria", FallbackCategory);
        return (predicted, true);
    }

    private static void Validate(TransactionRequest request)
    {
        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length is < 3 or > 200)
            throw new ValidationException("A descrição deve ter entre 3 e 200 caracteres.");
        if (request.Amount <= 0)
            throw new ValidationException("O valor deve ser maior que zero; o tipo vem da categoria.");
        if (request.Date == default)
            throw new ValidationException("Informe a data do lançamento.");
    }
}
