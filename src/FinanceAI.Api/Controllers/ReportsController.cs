using FinanceAI.Application.Dtos;
using FinanceAI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceAI.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet("monthly-summary/{year:int}/{month:int}")]
    public Task<MonthlySummaryResponse> MonthlySummary(int year, int month) =>
        service.GetMonthlySummaryAsync(year, month);
}
