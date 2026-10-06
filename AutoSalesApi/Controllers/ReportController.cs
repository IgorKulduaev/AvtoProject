using AutoSalesApi.Models.Dto;
using AutoSalesApi.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoSalesApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly ReportService service;

    public ReportController(ReportService _service)
    {
        service = _service;
    }

    // Список доступных отчётов.
    [HttpGet]
    public ActionResult<IEnumerable<string>> GetNames()
        => Ok(ReportService.ReportNames);

    // Построение отчёта по индексу (0..4).
    [HttpGet("{index:int}")]
    public async Task<ActionResult<ReportResult>> GetReport(int index)
    {
        if (index < 0 || index >= ReportService.ReportNames.Count)
            return BadRequest(new { message = $"Индекс отчёта должен быть от 0 до {ReportService.ReportNames.Count - 1}" });

        return Ok(await service.Build(index));
    }

    // Построение отчёта по имени (удобно для клиентов, знающих название).
    [HttpGet("by-name")]
    public async Task<ActionResult<ReportResult>> GetByName([FromQuery] string name)
    {
        var index = -1;
        for (var i = 0; i < ReportService.ReportNames.Count; i++)
        {
            if (string.Equals(ReportService.ReportNames[i], name, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index < 0) return NotFound(new { message = $"Отчёт '{name}' не найден" });
        return Ok(await service.Build(index));
    }
}
