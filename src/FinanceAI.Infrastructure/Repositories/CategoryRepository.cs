using Dapper;
using FinanceAI.Application.Abstractions;
using FinanceAI.Domain.Entities;
using Npgsql;

namespace FinanceAI.Infrastructure.Repositories;

public class CategoryRepository(NpgsqlDataSource dataSource) : ICategoryRepository
{
    private const string SelectColumns = "SELECT id, name, type FROM categories";

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return (await connection.QueryAsync<Category>($"{SelectColumns} ORDER BY type, name")).ToList();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Category>($"{SelectColumns} WHERE id = @id", new { id });
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Category>(
            $"{SelectColumns} WHERE LOWER(name) = LOWER(@name)", new { name });
    }

    public async Task<int> CreateAsync(Category category)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO categories (name, type) VALUES (@Name, @Type) RETURNING id", category);
    }

    public async Task UpdateAsync(Category category)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("UPDATE categories SET name = @Name, type = @Type WHERE id = @Id", category);
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM categories WHERE id = @id", new { id });
    }

    public async Task<bool> HasTransactionsAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM transactions WHERE category_id = @id)", new { id });
    }
}
