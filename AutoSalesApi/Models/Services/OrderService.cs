using AutoSalesApi.Models;
using AutoSalesApi.Models.Abstractions;
using AutoSalesApi.Models.Data;
using AutoSalesApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Services;

public class OrderService : AbstractionService, ICommonService<Order, int>
{
    private readonly AutoSalesDbContext db;

    public OrderService(AutoSalesDbContext db)
    {
        this.db = db;
    }

    public bool Create(Order model)
    {
        return DoAction(() =>
        {
            if (model.OrderDate == default)
                model.OrderDate = DateTime.Now;

            // Если стоимость не передана, рассчитываем по прейскуранту модели.
            if (model.TotalCost == 0)
            {
                var price = db.PriceLists.FirstOrDefault(p => p.ModelId == model.ModelId);
                if (price != null)
                    model.TotalCost = price.Price + price.PrepCost + price.TransportCost;
            }

            db.Orders.Add(model);
            db.SaveChanges();
        });
    }

    public bool Update(int id, Order model)
    {
        return DoAction(() =>
        {
            var res = db.Orders.First(o => o.OrderId == id);
            res.OrderNumber = model.OrderNumber;
            res.ClientId = model.ClientId;
            res.ModelId = model.ModelId;
            res.OrderDate = model.OrderDate;
            res.TotalCost = model.TotalCost;
            db.Orders.Update(res);
            db.SaveChanges();
        });
    }

    public bool Delete(int id)
    {
        return DoAction(() =>
        {
            var res = db.Orders.First(o => o.OrderId == id);
            db.Orders.Remove(res);
            db.SaveChanges();
        });
    }

    public async Task<Order?> Get(int id)
    {
        return await db.Orders
            .Include(o => o.Client)
            .Include(o => o.Model)
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    public async Task<IEnumerable<Order>> GetAll()
    {
        return await db.Orders
            .Include(o => o.Client)
            .Include(o => o.Model)
            .OrderBy(o => o.OrderId)
            .ToListAsync();
    }

    public async Task<IEnumerable<OrderItem>> GetItems()
    {
        return await db.Orders
            .Include(o => o.Client)
            .Include(o => o.Model)
            .OrderBy(o => o.OrderId)
            .Select(o => new OrderItem
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                ClientFio = o.Client!.FIO,
                ModelName = o.Model!.ModelName,
                OrderDate = o.OrderDate,
                TotalCost = o.TotalCost
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ModelWithPrice>> GetModelsWithPrice()
    {
        return await db.Models
            .Include(m => m.PriceList)
            .Where(m => m.PriceList != null)
            .OrderBy(m => m.ModelName)
            .Select(m => new ModelWithPrice
            {
                ModelId = m.ModelId,
                ModelName = m.ModelName,
                Color = m.Color,
                Transmission = m.Transmission,
                YearOfManufacture = m.PriceList!.YearOfManufacture,
                TotalCost = m.PriceList!.Price + m.PriceList!.PrepCost + m.PriceList!.TransportCost
            })
            .ToListAsync();
    }

    public async Task<string> NextOrderNumber()
    {
        var numbers = await db.Orders
            .Where(o => o.OrderNumber.StartsWith("Д"))
            .Select(o => o.OrderNumber)
            .ToListAsync();

        var max = numbers
            .Select(n => int.TryParse(n[1..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"Д{max + 1:000}";
    }

    public async Task<bool> OrderNumberExists(string orderNumber)
    {
        return await db.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<bool> Exists(int id)
    {
        return await db.Orders.AnyAsync(o => o.OrderId == id);
    }
}
