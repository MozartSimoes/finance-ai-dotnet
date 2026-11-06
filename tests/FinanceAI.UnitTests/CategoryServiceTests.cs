using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using FinanceAI.Application.Services;
using FinanceAI.Domain.Entities;
using FinanceAI.Domain.Exceptions;
using Moq;

namespace FinanceAI.UnitTests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repository = new();
    private readonly CategoryService _service;

    public CategoryServiceTests() => _service = new CategoryService(_repository.Object);

    [Fact]
    public async Task Create_DuplicatedName_ThrowsDuplicated()
    {
        _repository.Setup(r => r.GetByNameAsync("Pets"))
            .ReturnsAsync(new Category { Id = 20, Name = "Pets", Type = TransactionType.Expense });

        await Assert.ThrowsAsync<DuplicatedException>(() =>
            _service.CreateAsync(new CategoryRequest(" Pets ", TransactionType.Expense)));
    }

    [Fact]
    public async Task Update_KeepingOwnName_IsAllowed()
    {
        var pets = new Category { Id = 20, Name = "Pets", Type = TransactionType.Expense };
        _repository.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(pets);
        _repository.Setup(r => r.GetByNameAsync("Pets")).ReturnsAsync(pets);

        var result = await _service.UpdateAsync(20, new CategoryRequest("Pets", TransactionType.Expense));

        Assert.Equal("Pets", result.Name);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    public async Task Create_InvalidName_ThrowsValidation(string name)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(new CategoryRequest(name, TransactionType.Expense)));
    }

    [Fact]
    public async Task Delete_CategoryWithTransactions_ThrowsValidation()
    {
        _repository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Category { Id = 4, Name = "Transporte" });
        _repository.Setup(r => r.HasTransactionsAsync(4)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(4));
        _repository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
}
