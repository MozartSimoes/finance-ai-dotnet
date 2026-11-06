using FinanceAI.Application.Dtos;
using FinanceAI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAI.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<TransactionResponse>> Search([FromQuery] TransactionFilter filter) =>
        service.SearchAsync(filter);

    [HttpGet("{id:long}")]
    public Task<TransactionResponse> GetById(long id) => service.GetByIdAsync(id);

    /// <summary>Creates a transaction. Without <c>categoryId</c>, the ML model picks the category.</summary>
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TransactionResponse>> Create(TransactionRequest request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<TransactionResponse> Update(long id, TransactionRequest request) => service.UpdateAsync(id, request);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Previews the category the model would choose, without saving anything.</summary>
    [HttpPost("classify")]
    public ClassificationResult Classify(ClassifyRequest request) => service.Classify(request.Description);

    public record ClassifyRequest(string Description);
}
