using FinanceAI.Application.Dtos;
using FinanceAI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAI.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CategoryResponse>> GetAll() => service.GetAllAsync();

    [HttpGet("{id:int}")]
    public Task<CategoryResponse> GetById(int id) => service.GetByIdAsync(id);

    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public Task<CategoryResponse> Update(int id, CategoryRequest request) => service.UpdateAsync(id, request);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
