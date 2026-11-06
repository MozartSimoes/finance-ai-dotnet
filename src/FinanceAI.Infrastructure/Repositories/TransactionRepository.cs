using Dapper;
using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Domain.Entities;
using Npgsql;

namespace FinanceAI.Infrastructure.Repositories;

public class TransactionRepository(NpgsqlDataSource dataSource) : ITransactionRepository
{
    private const string SelectWithCategory = """
        SELECT t.id, t.description, t.amount, t.date, c.type, t.category_id,
               c.name AS category_name, t.category_suggested_by_ai
        FROM transactions t
        JOIN categories c ON c.id = t.category_id
        """;

    public async Task<TransactionResponse?> GetByIdAsync(long id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<TransactionRow>(
            $"{SelectWithCategory} WHERE t.id = @id", new { id });
        return row?.ToResponse();
    }

    public async Task<PagedResult<TransactionResponse>> SearchAsync(TransactionFilter filter)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.From is DateTime from)
        {
            where.Add("t.date >= @From::date");
            parameters.Add("From", from.Date);
        }
        if (filter.To is DateTime to)
        {
            where.Add("t.date <= @To::date");
            parameters.Add("To", to.Date);
        }
        if (filter.CategoryId is int categoryId)
        {
            where.Add("t.category_id = @CategoryId");
            parameters.Add("CategoryId", categoryId);
        }
        parameters.Add("Limit", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        // COUNT(*) OVER() returns the total alongside the page, saving a second round trip.
        var sql = $"""
            SELECT q.*, COUNT(*) OVER() AS total_items FROM ({SelectWithCategory}
            {(where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "")}) q
            ORDER BY q.date DESC, q.id DESC
            LIMIT @Limit OFFSET @Offset
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = (await connection.QueryAsync<TransactionRow>(sql, parameters)).ToList();
        var total = rows.Count > 0 ? (int)rows[0].TotalItems : 0;

        return new PagedResult<TransactionResponse>(
            rows.Select(r => r.ToResponse()).ToList(), filter.Page, filter.PageSize, total);
    }

    public async Task<long> CreateAsync(Transaction transaction)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO transactions (description, amount, date, category_id, category_suggested_by_ai)
            VALUES (@Description, @Amount, @Date::date, @CategoryId, @CategorySuggestedByAi)
            RETURNING id
            """, transaction);
    }

    public async Task<bool> UpdateAsync(Transaction transaction)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteAsync("""
            UPDATE transactions
            SET description = @Description, amount = @Amount, date = @Date::date,
                category_id = @CategoryId, category_suggested_by_ai = @CategorySuggestedByAi
            WHERE id = @Id
            """, transaction) > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteAsync("DELETE FROM transactions WHERE id = @id", new { id }) > 0;
    }

    public async Task<IReadOnlyList<CategoryTotal>> GetTotalsByCategoryAsync(DateTime from, DateTime to)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<CategoryTotalRow>("""
            SELECT c.name AS category, c.type, SUM(t.amount) AS total, COUNT(*) AS count
            FROM transactions t
            JOIN categories c ON c.id = t.category_id
            WHERE t.date BETWEEN @from::date AND @to::date
            GROUP BY c.name, c.type
            """, new { from = from.Date, to = to.Date });
        return rows.Select(r => new CategoryTotal(r.Category, r.Type, r.Total, (int)r.Count)).ToList();
    }

    // Flat rows keep Dapper mapping simple; records are built afterwards.
    private sealed class TransactionRow
    {
        public long Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool CategorySuggestedByAi { get; set; }
        public long TotalItems { get; set; }

        public TransactionResponse ToResponse() =>
            new(Id, Description, Amount, Date, Type, CategoryId, CategoryName, CategorySuggestedByAi);
    }

    private sealed class CategoryTotalRow
    {
        public string Category { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Total { get; set; }
        public long Count { get; set; }
    }
}
