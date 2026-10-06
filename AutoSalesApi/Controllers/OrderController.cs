using AutoSalesApi.Models;
using AutoSalesApi.Models.Dto;
using AutoSalesApi.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoSalesApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly OrderService service;

    public OrderController(OrderService _service)
    {
        service = _service;
    }

    [HttpGet]
    [Produces(typeof(OrderItem[]))]
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetAll()
        => Ok(await service.GetItems());

    [HttpGet("models-with-price")]
    public async Task<ActionResult<IEnumerable<ModelWithPrice>>> GetModelsWithPrice()
        => Ok(await service.GetModelsWithPrice());

    [HttpGet("next-number")]
    public async Task<ActionResult<object>> GetNextNumber()
        => Ok(new { orderNumber = await service.NextOrderNumber() });

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any, VaryByHeader = "User-Agent")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var res = await service.Get(id);
        return res == null ? NotFound(new { message = "Order not found" }) : Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] Order order)
    {
        if (string.IsNullOrWhiteSpace(order.OrderNumber))
            return BadRequest(new { message = "Введите номер договора" });

        if (await service.OrderNumberExists(order.OrderNumber))
            return BadRequest(new { message = "Договор с таким номером уже существует" });

        if (service.Create(order))
            return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
        return BadRequest(new { message = "Не удалось создать заказ (проверьте клиента и модель)" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await service.Exists(id)) return NotFound();
        if (service.Delete(id)) return NoContent();
        return BadRequest();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Order>> Update(int id, [FromBody] Order order)
    {
        if (order.OrderId != 0 && order.OrderId != id) return BadRequest();
        if (!await service.Exists(id)) return NotFound();
        if (service.Update(id, order)) return Ok(order);
        return BadRequest();
    }
}
