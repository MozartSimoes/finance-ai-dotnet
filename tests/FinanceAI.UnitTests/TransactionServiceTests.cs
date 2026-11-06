using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Application.Services;
using FinanceAI.Domain.Entities;
using FinanceAI.Domain.Exceptions;
using Moq;

namespace FinanceAI.UnitTests;

public class TransactionServiceTests
{
    private static readonly Category Transporte = new() { Id = 4, Name = "Transporte", Type = TransactionType.Expense };
    private static readonly Category Outros = new() { Id = 11, Name = "Outros", Type = TransactionType.Expense };

    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<ITransactionClassifier> _classifier = new();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _categories.Setup(r => r.GetByIdAsync(Transporte.Id)).ReturnsAsync(Transporte);
        _categories.Setup(r => r.GetByNameAsync("Transporte")).ReturnsAsync(Transporte);
        _categories.Setup(r => r.GetByNameAsync("Outros")).ReturnsAsync(Outros);
        _transactions.Setup(r => r.CreateAsync(It.IsAny<Transaction>())).ReturnsAsync(1);
        _transactions.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new TransactionResponse(1, "x", 10, DateTime.Today, TransactionType.Expense, 4, "Transporte", false));

        _service = new TransactionService(_transactions.Object, _categories.Object, _classifier.Object);
    }

    [Fact]
    public async Task Create_WithCategory_UsesItAndSkipsTheModel()
    {
        await _service.CreateAsync(new TransactionRequest("UBER *TRIP", 25.90m, DateTime.Today, Transporte.Id));

        _classifier.Verify(c => c.Classify(It.IsAny<string>()), Times.Never);
        _transactions.Verify(r => r.CreateAsync(It.Is<Transaction>(t =>
            t.CategoryId == Transporte.Id && !t.CategorySuggestedByAi)));
    }

    [Fact]
    public async Task Create_WithoutCategory_UsesTheModelPrediction()
    {
        _classifier.Setup(c => c.Classify("UBER *TRIP")).Returns(new ClassificationResult("Transporte", 0.91f));

        await _service.CreateAsync(new TransactionRequest("  UBER *TRIP ", 25.90m, DateTime.Today, null));

        _transactions.Verify(r => r.CreateAsync(It.Is<Transaction>(t =>
            t.CategoryId == Transporte.Id && t.CategorySuggestedByAi && t.Description == "UBER *TRIP")));
    }

    [Fact]
    public async Task Create_LowConfidencePrediction_FallsBackToOutros()
    {
        _classifier.Setup(c => c.Classify(It.IsAny<string>())).Returns(new ClassificationResult("Transporte", 0.2f));

        await _service.CreateAsync(new TransactionRequest("PAGAMENTO DIVERSO", 50m, DateTime.Today, null));

        _transactions.Verify(r => r.CreateAsync(It.Is<Transaction>(t => t.CategoryId == Outros.Id)));
    }

    [Theory]
    [InlineData("UBER", 0)]
    [InlineData("UBER", -10)]
    [InlineData("  ", 10)]
    public async Task Create_InvalidData_ThrowsValidation(string description, decimal amount)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(new TransactionRequest(description, amount, DateTime.Today, null)));
        _transactions.Verify(r => r.CreateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Create_UnknownCategory_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(new TransactionRequest("UBER *TRIP", 10m, DateTime.Today, 999)));
    }

    [Fact]
    public async Task Search_ClampsPageSize()
    {
        await _service.SearchAsync(new TransactionFilter(null, null, null, Page: 0, PageSize: 1000));

        _transactions.Verify(r => r.SearchAsync(It.Is<TransactionFilter>(f => f.Page == 1 && f.PageSize == 100)));
    }

    [Fact]
    public async Task Search_FromAfterTo_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.SearchAsync(
            new TransactionFilter(new DateTime(2026, 2, 1), new DateTime(2026, 1, 1), null)));
    }
}
