using AutoSalesApi.Models;
using AutoSalesApi.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AutoSalesApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProducerController : ControllerBase
{
    private readonly ILogger<ProducerController> logger;
    private readonly ProducerService service;
    private readonly IMemoryCache memoryCache;
    private const string CacheKey = "producers_page";

    public ProducerController(ProducerService _service, ILogger<ProducerController> _logger, IMemoryCache _memoryCache)
    {
        service = _service;
        logger = _logger;
        memoryCache = _memoryCache;
    }

    [HttpGet]
    [Produces(typeof(Producer[]))]
    public async Task<ActionResult<IEnumerable<Producer>>> GetPage(int? page)
    {
        var key = $"{CacheKey}_{page ?? 1}";
        if (!memoryCache.TryGetValue(key, out IEnumerable<Producer>? cachedValue))
        {
            cachedValue = await service.GetPage(page);
            memoryCache.Set(key, cachedValue, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(5),
                Size = cachedValue.ToArray().Length
            });
        }

        var stats = memoryCache.GetCurrentStatistics();
        logger.LogInformation($"Memory cache. Total hits: {stats?.TotalHits}. Estimated size: {stats?.CurrentEstimatedSize}.");
        return Ok(cachedValue ?? Enumerable.Empty<Producer>());
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<Producer>>> GetAll()
        => Ok(await service.GetAll());

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByHeader = "User-Agent")]
    public async Task<ActionResult<Producer>> GetById(int id)
    {
        var res = await service.Get(id);
        return res == null ? NotFound(new { message = "Producer not found" }) : Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<Producer>> Create([FromBody] Producer producer)
    {
        if (service.Create(producer))
            return CreatedAtAction(nameof(GetById), new { id = producer.ProducerId }, producer);
        return BadRequest();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await service.Exists(id)) return NotFound();
        if (service.Delete(id)) return NoContent();
        return BadRequest(new { message = "Не удалось удалить поставщика (возможно, есть связанные заказы)" });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Producer>> Update(int id, [FromBody] Producer producer)
    {
        if (producer.ProducerId != 0 && producer.ProducerId != id) return BadRequest();
        if (!await service.Exists(id)) return NotFound();
        if (service.Update(id, producer)) return Ok(producer);
        return BadRequest();
    }
}
