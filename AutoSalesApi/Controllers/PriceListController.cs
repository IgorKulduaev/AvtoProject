using AutoSalesApi.Models;
using AutoSalesApi.Models.Dto;
using AutoSalesApi.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoSalesApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PriceListController : ControllerBase
{
    private readonly PriceListService service;
    private readonly ModelService modelService;

    public PriceListController(PriceListService _service, ModelService _modelService)
    {
        service = _service;
        modelService = _modelService;
    }

    [HttpGet]
    [Produces(typeof(PriceListItem[]))]
    public async Task<ActionResult<IEnumerable<PriceListItem>>> GetAll()
        => Ok(await service.GetItems());

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByHeader = "User-Agent")]
    public async Task<ActionResult<PriceList>> GetById(int id)
    {
        var res = await service.Get(id);
        return res == null ? NotFound(new { message = "PriceList not found" }) : Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<PriceList>> Create([FromBody] PriceList priceList)
    {
        if (!await modelService.Exists(priceList.ModelId))
            return BadRequest(new { message = "Модель не найдена" });

        if (await service.ExistsForModel(priceList.ModelId))
            return BadRequest(new { message = "Для этой модели уже есть прейскурант (связь 1:1)" });

        if (service.Create(priceList))
            return CreatedAtAction(nameof(GetById), new { id = priceList.PriceId }, priceList);
        return BadRequest();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await service.Exists(id)) return NotFound();
        if (service.Delete(id)) return NoContent();
        return BadRequest();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PriceList>> Update(int id, [FromBody] PriceList priceList)
    {
        if (priceList.PriceId != 0 && priceList.PriceId != id) return BadRequest();
        if (!await service.Exists(id)) return NotFound();
        if (service.Update(id, priceList)) return Ok(priceList);
        return BadRequest();
    }
}
