using AutoSalesApi.Models;
using AutoSalesApi.Models.Abstractions;
using AutoSalesApi.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Services;

public class ProducerService : AbstractionService, ICommonService<Producer, int>
{
    private readonly AutoSalesDbContext db;

    public ProducerService(AutoSalesDbContext db)
    {
        this.db = db;
    }

    public bool Create(Producer model)
    {
        return DoAction(() =>
        {
            db.Producers.Add(model);
            db.SaveChanges();
        });
    }

    public bool Update(int id, Producer model)
    {
        return DoAction(() =>
        {
            var res = db.Producers.First(p => p.ProducerId == id);
            res.CompanyCode = model.CompanyCode;
            res.CompanyName = model.CompanyName;
            res.Phone = model.Phone;
            res.Email = model.Email;
            res.Website = model.Website;
            db.Producers.Update(res);
            db.SaveChanges();
        });
    }

    public bool Delete(int id)
    {
        return DoAction(() =>
        {
            var res = db.Producers.First(p => p.ProducerId == id);
            db.Producers.Remove(res);
            db.SaveChanges();
        });
    }

    public async Task<Producer?> Get(int id)
    {
        return await db.Producers
            .Include(p => p.Offers)
            .FirstOrDefaultAsync(p => p.ProducerId == id);
    }

    public async Task<IEnumerable<Producer>> GetAll()
    {
        return await db.Producers.OrderBy(p => p.CompanyName).ToListAsync();
    }

    public async Task<IEnumerable<Producer>> GetPage(int? page)
    {
        const int pageSize = 10;
        return await db.Producers
            .OrderBy(p => p.ProducerId)
            .Skip(((page ?? 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> Exists(int id)
    {
        return await db.Producers.AnyAsync(p => p.ProducerId == id);
    }
}
