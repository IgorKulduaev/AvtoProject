using AutoSalesApi.Models;
using AutoSalesApi.Models.Abstractions;
using AutoSalesApi.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Services;

public class ClientService : AbstractionService, ICommonService<Client, int>
{
    private readonly AutoSalesDbContext db;

    public ClientService(AutoSalesDbContext db)
    {
        this.db = db;
    }

    public bool Create(Client model)
    {
        return DoAction(() =>
        {
            db.Clients.Add(model);
            db.SaveChanges();
        });
    }

    public bool Update(int id, Client model)
    {
        return DoAction(() =>
        {
            var res = db.Clients.First(c => c.ClientId == id);
            res.FIO = model.FIO;
            res.Phone = model.Phone;
            res.Address = model.Address;
            db.Clients.Update(res);
            db.SaveChanges();
        });
    }

    public bool Delete(int id)
    {
        return DoAction(() =>
        {
            var res = db.Clients.First(c => c.ClientId == id);
            db.Clients.Remove(res);
            db.SaveChanges();
        });
    }

    public async Task<Client?> Get(int id)
    {
        return await db.Clients.FirstOrDefaultAsync(c => c.ClientId == id);
    }

    public async Task<IEnumerable<Client>> GetAll()
    {
        return await db.Clients.OrderBy(c => c.FIO).ToListAsync();
    }

    public async Task<IEnumerable<Client>> GetPage(int? page)
    {
        const int pageSize = 10;
        return await db.Clients
            .OrderBy(c => c.ClientId)
            .Skip(((page ?? 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> Exists(int id)
    {
        return await db.Clients.AnyAsync(c => c.ClientId == id);
    }
}
