using AutoSalesApi.Models;
using AutoSalesApi.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoSalesApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientController : ControllerBase
{
    private readonly ClientService service;

    public ClientController(ClientService _service)
    {
        service = _service;
    }

    [HttpGet]
    [Produces(typeof(Client[]))]
    public async Task<ActionResult<IEnumerable<Client>>> GetPage(int? page)
        => Ok(await service.GetPage(page));

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<Client>>> GetAll()
        => Ok(await service.GetAll());

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByHeader = "User-Agent")]
    public async Task<ActionResult<Client>> GetById(int id)
    {
        var res = await service.Get(id);
        return res == null ? NotFound(new { message = "Client not found" }) : Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create([FromBody] Client client)
    {
        if (service.Create(client))
            return CreatedAtAction(nameof(GetById), new { id = client.ClientId }, client);
        return BadRequest();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await service.Exists(id)) return NotFound();
        if (service.Delete(id)) return NoContent();
        return BadRequest(new { message = "Не удалось удалить клиента (возможно, есть связанные заказы)" });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Client>> Update(int id, [FromBody] Client client)
    {
        if (client.ClientId != 0 && client.ClientId != id) return BadRequest();
        if (!await service.Exists(id)) return NotFound();
        if (service.Update(id, client)) return Ok(client);
        return BadRequest();
    }
}
