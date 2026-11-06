namespace FinanceAI.Domain.Entities;

public class Transaction
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }

    /// <summary>True when the category was chosen by the ML model instead of the user.</summary>
    public bool CategorySuggestedByAi { get; set; }

    public DateTime CreatedAt { get; set; }
}
