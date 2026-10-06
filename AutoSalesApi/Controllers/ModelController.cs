using AutoSalesApi.Models;
using AutoSalesApi.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoSalesApi.Controllers;

public class ModelCreateRequest
{
    public Model Model { get; set; } = new();
    public PriceList? PriceList { get; set; }
    public List<int> ProducerIds { get; set; } = new();
}

public class ModelOffersRequest
{
    public List<int> ProducerIds { get; set; } = new();
}

[Route("api/[controller]")]
[ApiController]
public class ModelController : ControllerBase
{
    private readonly ModelService service;
    private readonly ProducerService producerService;

    public ModelController(ModelService _service, ProducerService _producerService)
    {
        service = _service;
        producerService = _producerService;
    }

    [HttpGet]
    [Produces(typeof(Model[]))]
    public async Task<ActionResult<IEnumerable<Model>>> GetPage(int? page)
        => Ok(await service.GetPage(page));

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<Model>>> GetAll()
        => Ok(await service.GetAll());

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByHeader = "User-Agent")]
    public async Task<ActionResult<Model>> GetById(int id)
    {
        var res = await service.Get(id);
        return res == null ? NotFound(new { message = "Model not found" }) : Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<Model>> Create([FromBody] Model model)
    {
        if (service.Create(model))
            return CreatedAtAction(nameof(GetById), new { id = model.ModelId }, model);
        return BadRequest();
    }

    // Создание модели вместе с прейскурантом и связями с поставщиками.
    [HttpPost("with-details")]
    public async Task<ActionResult<Model>> CreateWithDetails([FromBody] ModelCreateRequest request)
    {
        if (request.Model == null) return BadRequest();

        foreach (var producerId in request.ProducerIds.Distinct())
        {
            if (!await producerService.Exists(producerId))
                return BadRequest(new { message = $"Поставщик {producerId} не найден" });
        }

        if (!service.CreateWithDetails(request.Model, request.PriceList, request.ProducerIds))
            return BadRequest();

        return CreatedAtAction(nameof(GetById), new { id = request.Model.ModelId }, request.Model);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await service.Exists(id)) return NotFound();
        if (service.Delete(id)) return NoContent();
        return BadRequest(new { message = "Не удалось удалить модель (возможно, есть связанные заказы)" });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Model>> Update(int id, [FromBody] Model model)
    {
        if (model.ModelId != 0 && model.ModelId != id) return BadRequest();
        if (!await service.Exists(id)) return NotFound();
        if (service.Update(id, model)) return Ok(model);
        return BadRequest();
    }

    // Обновление связей модели с поставщиками (таблица Offer).
    [HttpPut("{id}/offers")]
    public async Task<ActionResult> UpdateOffers(int id, [FromBody] ModelOffersRequest request)
    {
        if (!await service.Exists(id)) return NotFound();
        if (service.UpdateOffers(id, request.ProducerIds)) return NoContent();
        return BadRequest();
    }
}
