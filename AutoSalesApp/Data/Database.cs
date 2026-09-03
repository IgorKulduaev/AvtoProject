using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AutoSalesApp.Models;

namespace AutoSalesApp.Data;

public static class Database
{
    public static void Init()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
        Seed(db);
    }

    private static void Seed(AppDbContext db)
    {
        if (db.Producers.Any() || db.Clients.Any() || db.Models.Any())
            return;

        var p1 = new Producer { CompanyCode = "APL", CompanyName = "АвтоПлюс", Phone = "+7 (911) 234-56-78", Email = "info@autoplus.ru", Website = "autoplus.ru" };
        var p2 = new Producer { CompanyCode = "GDK", CompanyName = "ГолдКар", Phone = "+7 (812) 456-78-90", Email = "sales@goldcar.com", Website = "goldcar.com" };

        var c1 = new Client { FIO = "Иванов И. П.", Phone = "+7 (900) 100-10-20", Address = "ул. Ленина, 10" };
        var c2 = new Client { FIO = "Петрова А. С.", Phone = "+7 (911) 234-56-78", Address = "пр. Мира, 5" };
        var c3 = new Client { FIO = "Сидоров В. А.", Phone = "+7 (922) 345-67-89", Address = "ул. Гагарина, 3" };
        var c4 = new Client { FIO = "Кузьмина О. Н.", Phone = "+7 (933) 456-78-90", Address = "ул. Октябрьская, 15" };
        var c5 = new Client { FIO = "Громов С. В.", Phone = "+7 (944) 567-89-01", Address = "ул. Победы, 22" };

        var m1 = new Model { ModelCode = "CAM001", ModelName = "Toyota Camry", Color = "Чёрный", Upholstery = "кожа", MotorPower = "150/200 кВт", DoorCount = 4, Transmission = "автоматическая" };
        var m2 = new Model { ModelCode = "COR002", ModelName = "Toyota Corolla", Color = "Белый", Upholstery = "ткань", MotorPower = "90/120 кВт", DoorCount = 4, Transmission = "механическая" };
        var m3 = new Model { ModelCode = "BMW003", ModelName = "BMW X5", Color = "Синий", Upholstery = "кожа", MotorPower = "200/300 кВт", DoorCount = 5, Transmission = "автоматическая" };
        var m4 = new Model { ModelCode = "AUD004", ModelName = "Audi A6", Color = "Серый", Upholstery = "кожа", MotorPower = "180/250 кВт", DoorCount = 4, Transmission = "автоматическая" };
        var m5 = new Model { ModelCode = "KIA005", ModelName = "Kia Rio", Color = "Красный", Upholstery = "ткань", MotorPower = "74/100 кВт", DoorCount = 4, Transmission = "механическая" };
        var m6 = new Model { ModelCode = "HYU006", ModelName = "Hyundai Tucson", Color = "Белый", Upholstery = "кожа", MotorPower = "130/180 кВт", DoorCount = 5, Transmission = "автоматическая" };

        db.Producers.AddRange(p1, p2);
        db.Clients.AddRange(c1, c2, c3, c4, c5);
        db.Models.AddRange(m1, m2, m3, m4, m5, m6);
        db.SaveChanges();

        db.PriceLists.AddRange(
            new PriceList { ModelId = m1.ModelId, YearOfManufacture = 2023, Price = 25000, PrepCost = 800, TransportCost = 1200 },
            new PriceList { ModelId = m2.ModelId, YearOfManufacture = 2024, Price = 18000, PrepCost = 500, TransportCost = 900 },
            new PriceList { ModelId = m3.ModelId, YearOfManufacture = 2023, Price = 55000, PrepCost = 1500, TransportCost = 2500 },
            new PriceList { ModelId = m4.ModelId, YearOfManufacture = 2022, Price = 43000, PrepCost = 1000, TransportCost = 1800 },
            new PriceList { ModelId = m5.ModelId, YearOfManufacture = 2024, Price = 12500, PrepCost = 300, TransportCost = 700 },
            new PriceList { ModelId = m6.ModelId, YearOfManufacture = 2024, Price = 22000, PrepCost = 600, TransportCost = 1300 });
        db.SaveChanges();

        db.Offers.AddRange(
            new Offer { ProducerId = p1.ProducerId, ModelId = m1.ModelId },
            new Offer { ProducerId = p1.ProducerId, ModelId = m2.ModelId },
            new Offer { ProducerId = p1.ProducerId, ModelId = m3.ModelId },
            new Offer { ProducerId = p2.ProducerId, ModelId = m4.ModelId },
            new Offer { ProducerId = p2.ProducerId, ModelId = m5.ModelId },
            new Offer { ProducerId = p2.ProducerId, ModelId = m6.ModelId });
        db.SaveChanges();

        var price = db.PriceLists.First(pl => pl.ModelId == m1.ModelId);
        db.Orders.Add(new Order
        {
            OrderNumber = "Д001",
            ClientId = c2.ClientId,
            ModelId = m1.ModelId,
            OrderDate = new DateTime(2025, 1, 15),
            TotalCost = price.Price + price.PrepCost + price.TransportCost
        });
        db.SaveChanges();
    }
}
