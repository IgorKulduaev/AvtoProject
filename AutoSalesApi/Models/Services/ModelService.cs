using AutoSalesApi.Models;
using AutoSalesApi.Models.Abstractions;
using AutoSalesApi.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Services;

public class ModelService : AbstractionService, ICommonService<Model, int>
{
    private readonly AutoSalesDbContext db;

    public ModelService(AutoSalesDbContext db)
    {
        this.db = db;
    }

    public bool Create(Model model)
    {
        return DoAction(() =>
        {
            db.Models.Add(model);
            db.SaveChanges();
        });
    }

    public bool Update(int id, Model model)
    {
        return DoAction(() =>
        {
            var res = db.Models.First(m => m.ModelId == id);
            res.ModelCode = model.ModelCode;
            res.ModelName = model.ModelName;
            res.Color = model.Color;
            res.Upholstery = model.Upholstery;
            res.MotorPower = model.MotorPower;
            res.DoorCount = model.DoorCount;
            res.Transmission = model.Transmission;
            db.Models.Update(res);
            db.SaveChanges();
        });
    }

    public bool Delete(int id)
    {
        return DoAction(() =>
        {
            var res = db.Models.First(m => m.ModelId == id);
            db.Models.Remove(res);
            db.SaveChanges();
        });
    }

    public async Task<Model?> Get(int id)
    {
        return await db.Models
            .Include(m => m.PriceList)
            .Include(m => m.Offers).ThenInclude(o => o.Producer)
            .FirstOrDefaultAsync(m => m.ModelId == id);
    }

    public async Task<IEnumerable<Model>> GetAll()
    {
        return await db.Models
            .Include(m => m.PriceList)
            .OrderBy(m => m.ModelName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Model>> GetPage(int? page)
    {
        const int pageSize = 10;
        return await db.Models
            .OrderBy(m => m.ModelId)
            .Skip(((page ?? 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // Создание модели вместе с прейскурантом и связями с поставщиками (аналог ModelViewModel.AddModel).
    public bool CreateWithDetails(Model model, PriceList? priceList, IEnumerable<int> producerIds)
    {
        return DoAction(() =>
        {
            db.Models.Add(model);
            db.SaveChanges();

            if (priceList != null)
            {
                priceList.ModelId = model.ModelId;
                db.PriceLists.Add(priceList);
            }

            foreach (var producerId in producerIds.Distinct())
                db.Offers.Add(new Offer { ProducerId = producerId, ModelId = model.ModelId });

            db.SaveChanges();
        });
    }

    // Обновление связей модели с поставщиками.
    public bool UpdateOffers(int id, IEnumerable<int> producerIds)
    {
        return DoAction(() =>
        {
            var existing = db.Offers.Where(o => o.ModelId == id).ToList();
            db.Offers.RemoveRange(existing);

            foreach (var producerId in producerIds.Distinct())
                db.Offers.Add(new Offer { ProducerId = producerId, ModelId = id });

            db.SaveChanges();
        });
    }

    public async Task<bool> Exists(int id)
    {
        return await db.Models.AnyAsync(m => m.ModelId == id);
    }
}
