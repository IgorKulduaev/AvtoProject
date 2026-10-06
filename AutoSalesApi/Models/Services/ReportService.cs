using System.Globalization;
using AutoSalesApi.Models.Data;
using AutoSalesApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApi.Models.Services;

public class ReportService
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private readonly AutoSalesDbContext db;

    public ReportService(AutoSalesDbContext db)
    {
        this.db = db;
    }

    public static IReadOnlyList<string> ReportNames { get; } = new[]
    {
        "Сводка по поставщикам",
        "Сводка по моделям",
        "Сводка по клиентам",
        "Непроданные модели",
        "Доходность по моделям"
    };

    public async Task<ReportResult> Build(int index)
    {
        return index switch
        {
            0 => await ProducerSummary(),
            1 => await ModelSummary(),
            2 => await ClientSummary(),
            3 => await UnsoldModels(),
            _ => await ModelProfitability()
        };
    }

    private static string Money(decimal? value)
        => value?.ToString("N2", Ru) ?? "-";

    // Отчёт 1. Сводка по поставщикам
    private async Task<ReportResult> ProducerSummary()
    {
        var data = await db.Producers
            .Select(p => new
            {
                p.CompanyName,
                p.Phone,
                p.Email,
                ModelsCount = p.Offers.Select(o => o.ModelId).Distinct().Count(),
                SoldCount = p.Offers.SelectMany(o => o.Model!.Orders).Count()
            })
            .OrderBy(x => x.CompanyName)
            .ToListAsync();

        return new ReportResult
        {
            Headers = new[] { "Название фирмы", "Телефон", "Email", "Моделей предлагает", "Продано автомобилей" },
            Rows = data.Select(x => (IReadOnlyList<string>)new[]
            {
                x.CompanyName, x.Phone, x.Email ?? "", x.ModelsCount.ToString(), x.SoldCount.ToString()
            }).ToList()
        };
    }

    // Отчёт 2. Сводка по моделям
    private async Task<ReportResult> ModelSummary()
    {
        var data = await db.Models
            .Select(m => new
            {
                m.ModelName,
                m.Color,
                m.Upholstery,
                m.MotorPower,
                m.Transmission,
                Year = m.PriceList != null ? (int?)m.PriceList.YearOfManufacture : null,
                Price = m.PriceList != null ? (decimal?)m.PriceList.Price : null,
                TotalCost = m.PriceList != null
                    ? (decimal?)(m.PriceList.Price + m.PriceList.PrepCost + m.PriceList.TransportCost)
                    : null,
                SoldCount = m.Orders.Count
            })
            .OrderBy(x => x.ModelName)
            .ToListAsync();

        return new ReportResult
        {
            Headers = new[]
            {
                "Модель", "Цвет", "Обивка", "Мощность", "КПП", "Год", "Цена (у.е.)",
                "Полная стоимость (у.е.)", "Продаж"
            },
            Rows = data.Select(x => (IReadOnlyList<string>)new[]
            {
                x.ModelName, x.Color, x.Upholstery, x.MotorPower, x.Transmission,
                x.Year?.ToString() ?? "-", Money(x.Price), Money(x.TotalCost), x.SoldCount.ToString()
            }).ToList()
        };
    }

    // Отчёт 3. Сводка по клиентам
    private async Task<ReportResult> ClientSummary()
    {
        var data = await db.Clients
            .Select(c => new
            {
                c.FIO,
                c.Phone,
                c.Address,
                Orders = c.Orders
                    .OrderBy(o => o.OrderDate)
                    .Select(o => new { o.OrderDate, ModelName = o.Model!.ModelName, o.TotalCost })
                    .ToList()
            })
            .OrderBy(x => x.FIO)
            .ToListAsync();

        var rows = new List<IReadOnlyList<string>>();
        foreach (var c in data)
        {
            if (c.Orders.Count == 0)
            {
                rows.Add(new[] { c.FIO, c.Phone, c.Address, "-", "-", "-" });
                continue;
            }

            foreach (var o in c.Orders)
            {
                rows.Add(new[]
                {
                    c.FIO, c.Phone, c.Address,
                    o.OrderDate.ToString("dd.MM.yyyy"),
                    o.ModelName,
                    Money(o.TotalCost)
                });
            }
        }

        return new ReportResult
        {
            Headers = new[] { "Ф.И.О.", "Телефон", "Адрес", "Дата покупки", "Купленная модель", "Стоимость покупки (у.е.)" },
            Rows = rows
        };
    }

    // Отчёт 4. Непроданные модели
    private async Task<ReportResult> UnsoldModels()
    {
        var data = await db.Models
            .Where(m => !m.Orders.Any())
            .Select(m => new
            {
                m.ModelName,
                m.Color,
                m.Transmission,
                Year = m.PriceList != null ? (int?)m.PriceList.YearOfManufacture : null,
                TotalCost = m.PriceList != null
                    ? (decimal?)(m.PriceList.Price + m.PriceList.PrepCost + m.PriceList.TransportCost)
                    : null
            })
            .OrderBy(x => x.ModelName)
            .ToListAsync();

        return new ReportResult
        {
            Headers = new[] { "Модель", "Цвет", "КПП", "Год", "Полная стоимость (у.е.)" },
            Rows = data.Select(x => (IReadOnlyList<string>)new[]
            {
                x.ModelName, x.Color, x.Transmission, x.Year?.ToString() ?? "-", Money(x.TotalCost)
            }).ToList()
        };
    }

    // Отчёт 5. Доходность по моделям
    private async Task<ReportResult> ModelProfitability()
    {
        var data = await db.Models
            .Where(m => m.PriceList != null)
            .Select(m => new
            {
                m.ModelName,
                Price = m.PriceList!.Price,
                Prep = m.PriceList!.PrepCost,
                Transport = m.PriceList!.TransportCost,
                TotalCost = m.PriceList!.Price + m.PriceList!.PrepCost + m.PriceList!.TransportCost,
                SoldCount = m.Orders.Count
            })
            .OrderBy(x => x.ModelName)
            .ToListAsync();

        return new ReportResult
        {
            Headers = new[]
            {
                "Модель", "Цена (у.е.)", "Подготовка (у.е.)", "Транспорт (у.е.)",
                "Полная стоимость (у.е.)", "Продано", "Выручка (у.е.)"
            },
            Rows = data.Select(x => (IReadOnlyList<string>)new[]
            {
                x.ModelName, Money(x.Price), Money(x.Prep), Money(x.Transport),
                Money(x.TotalCost), x.SoldCount.ToString(), Money(x.TotalCost * x.SoldCount)
            }).ToList()
        };
    }
}
