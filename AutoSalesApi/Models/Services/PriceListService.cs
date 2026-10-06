using AutoSalesApi.Models;
using AutoSalesApi.Models.Abstractions;
using AutoSalesApi.Models.Data;
using AutoSalesApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Services;

public class PriceListService : AbstractionService, ICommonService<PriceList, int>
{
    private readonly AutoSalesDbContext db;

    public PriceListService(AutoSalesDbContext db)
    {
        this.db = db;
    }

    public bool Create(PriceList model)
    {
        return DoAction(() =>
        {
            db.PriceLists.Add(model);
            db.SaveChanges();
        });
    }

    public bool Update(int id, PriceList model)
    {
        return DoAction(() =>
        {
            var res = db.PriceLists.First(p => p.PriceId == id);
            res.YearOfManufacture = model.YearOfManufacture;
            res.Price = model.Price;
            res.PrepCost = model.PrepCost;
            res.TransportCost = model.TransportCost;
            db.PriceLists.Update(res);
            db.SaveChanges();
        });
    }

    public bool Delete(int id)
    {
        return DoAction(() =>
        {
            var res = db.PriceLists.First(p => p.PriceId == id);
            db.PriceLists.Remove(res);
            db.SaveChanges();
        });
    }

    public async Task<PriceList?> Get(int id)
    {
        return await db.PriceLists
            .Include(p => p.Model)
            .FirstOrDefaultAsync(p => p.PriceId == id);
    }

    public async Task<IEnumerable<PriceList>> GetAll()
    {
        return await db.PriceLists.Include(p => p.Model).OrderBy(p => p.PriceId).ToListAsync();
    }

    public async Task<IEnumerable<PriceListItem>> GetItems()
    {
        return await db.PriceLists
            .Include(p => p.Model)
            .OrderBy(p => p.PriceId)
            .Select(p => new PriceListItem
            {
                PriceId = p.PriceId,
                ModelId = p.ModelId,
                ModelName = p.Model!.ModelName,
                YearOfManufacture = p.YearOfManufacture,
                Price = p.Price,
                PrepCost = p.PrepCost,
                TransportCost = p.TransportCost,
                TotalCost = p.Price + p.PrepCost + p.TransportCost
            })
            .ToListAsync();
    }

    public async Task<bool> ExistsForModel(int modelId)
    {
        return await db.PriceLists.AnyAsync(p => p.ModelId == modelId);
    }

    public async Task<bool> Exists(int id)
    {
        return await db.PriceLists.AnyAsync(p => p.PriceId == id);
    }
}
