using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Domain.Entities;
using FinanceAI.Domain.Exceptions;

namespace FinanceAI.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync();
    Task<CategoryResponse> GetByIdAsync(int id);
    Task<CategoryResponse> CreateAsync(CategoryRequest request);
    Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request);
    Task DeleteAsync(int id);
}

public class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync() =>
        (await repository.GetAllAsync()).Select(ToResponse).ToList();

    public async Task<CategoryResponse> GetByIdAsync(int id) => ToResponse(await GetExistingAsync(id));

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request)
    {
        var name = Validate(request);
        await EnsureNameIsAvailableAsync(name, currentId: null);

        var category = new Category { Name = name, Type = request.Type };
        category.Id = await repository.CreateAsync(category);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request)
    {
        var name = Validate(request);
        var category = await GetExistingAsync(id);
        await EnsureNameIsAvailableAsync(name, currentId: id);

        category.Name = name;
        category.Type = request.Type;
        await repository.UpdateAsync(category);
        return ToResponse(category);
    }

    public async Task DeleteAsync(int id)
    {
        await GetExistingAsync(id);
        if (await repository.HasTransactionsAsync(id))
            throw new ValidationException("Categoria possui lançamentos e não pode ser excluída.");

        await repository.DeleteAsync(id);
    }

    private async Task<Category> GetExistingAsync(int id) =>
        await repository.GetByIdAsync(id) ?? throw new NotFoundException("Categoria", id);

    private async Task EnsureNameIsAvailableAsync(string name, int? currentId)
    {
        var existing = await repository.GetByNameAsync(name);
        if (existing is not null && existing.Id != currentId)
            throw new DuplicatedException($"Já existe uma categoria chamada '{name}'.");
    }

    private static string Validate(CategoryRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is < 2 or > 60)
            throw new ValidationException("O nome da categoria deve ter entre 2 e 60 caracteres.");
        if (!Enum.IsDefined(request.Type))
            throw new ValidationException("Tipo de categoria inválido.");
        return name;
    }

    private static CategoryResponse ToResponse(Category c) => new(c.Id, c.Name, c.Type);
}
