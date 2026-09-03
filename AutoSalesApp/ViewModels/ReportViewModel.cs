using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AutoSalesApp.Data;
using AutoSalesApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApp.ViewModels;
public partial class ReportViewModel : ObservableObject
{
    public ObservableCollection<string> ReportNames { get; } = new()
    {
        "Сводка по поставщикам",
        "Сводка по моделям",
        "Сводка по клиентам",
        "Непроданные модели",
        "Доходность по моделям"
    };

    [ObservableProperty]
    private int _selectedReportIndex;

    [ObservableProperty]
    private ObservableCollection<string> _headers = new();

    [ObservableProperty]
    private ObservableCollection<ReportRow> _rows = new();

    public ReportViewModel()
    {
        BuildReport();
    }

    partial void OnSelectedReportIndexChanged(int value)
    {
        BuildReport();
    }

    private void BuildReport()
    {
        using var db = new AppDbContext();

        switch (SelectedReportIndex)
        {
            case 0: // Сводка по поставщикам
            {
                Headers = new ObservableCollection<string>
                {
                    "Название фирмы", "Телефон", "Email", "Моделей предлагает", "Продано автомобилей"
                };
                var data = db.Producers
                    .OrderBy(p => p.CompanyName)
                    .ToList()
                    .Select(p => new
                    {
                        p.CompanyName,
                        p.Phone,
                        p.Email,
                        ModelsCount = db.Offers.Count(o => o.ProducerId == p.ProducerId),
                        SoldCount = db.Offers
                            .Where(o => o.ProducerId == p.ProducerId)
                            .Select(o => o.ModelId)
                            .Distinct()
                            .Sum(mid => db.Orders.Count(o2 => o2.ModelId == mid))
                    });
                Rows = ToRows(data, r =>
                {
                    var companyName = (string)r.CompanyName;
                    var phone = (string)r.Phone;
                    var email = (string?)r.Email ?? "";
                    var models = ((int)r.ModelsCount).ToString();
                    var sold = ((int)r.SoldCount).ToString();
                    return new string?[] { companyName, phone, email, models, sold };
                });
                break;
            }
            case 1: // Сводка по моделям
            {
                Headers = new ObservableCollection<string>
                {
                    "Модель", "Цвет", "Обивка", "Мощность", "КПП", "Год", "Цена (у.е.)",
                    "Полная стоимость (у.е.)", "Продаж"
                };
                var data = db.Models
                    .Include(m => m.PriceList)
                    .OrderBy(m => m.ModelName)
                    .ToList()
                    .Select(m => new
                    {
                        m.ModelName,
                        m.Color,
                        m.Upholstery,
                        m.MotorPower,
                        m.Transmission,
                        YearOfManufacture = (int?)m.PriceList?.YearOfManufacture,
                        Price = (decimal?)m.PriceList?.Price,
                        TotalCost = m.PriceList == null
                            ? (decimal?)null
                            : m.PriceList.Price + m.PriceList.PrepCost + m.PriceList.TransportCost,
                        SoldCount = db.Orders.Count(o => o.ModelId == m.ModelId)
                    });
                Rows = ToRows(data, r =>
                {
                    var name = (string)r.ModelName;
                    var color = (string)r.Color;
                    var upholstery = (string)r.Upholstery;
                    var power = (string)r.MotorPower;
                    var transmission = (string)r.Transmission;
                    var year = r.YearOfManufacture?.ToString() ?? "-";
                    var price = Money((decimal?)r.Price);
                    var total = Money((decimal?)r.TotalCost);
                    var sold = ((int)r.SoldCount).ToString();
                    return new string?[] { name, color, upholstery, power, transmission, year, price, total, sold };
                });
                break;
            }
            case 2: // Сводка по клиентам
            {
                Headers = new ObservableCollection<string>
                {
                    "Ф.И.О.", "Телефон", "Адрес", "Дата покупки", "Купленная модель", "Стоимость покупки (у.е.)"
                };
                var clients = db.Clients.OrderBy(c => c.FIO).ToList();
                var rows = new List<ReportRow>();
                foreach (var cli in clients)
                {
                    var orders = db.Orders
                        .Include(o => o.Model)
                        .Where(o => o.ClientId == cli.ClientId)
                        .OrderBy(o => o.OrderDate)
                        .ToList();

                    if (orders.Count == 0)
                    {
                        rows.Add(new ReportRow { Cells = new[] { cli.FIO, cli.Phone, cli.Address, "-", "-", "-" } });
                    }
                    else
                    {
                        foreach (var order in orders)
                        {
                            rows.Add(new ReportRow
                            {
                                Cells = new[]
                                {
                                    cli.FIO, cli.Phone, cli.Address,
                                    order.OrderDate.ToString("dd.MM.yyyy"),
                                    order.Model?.ModelName ?? "-",
                                    Money(order.TotalCost)
                                }
                            });
                        }
                    }
                }
                Rows = new ObservableCollection<ReportRow>(rows);
                break;
            }
            case 3: // Непроданные модели
            {
                Headers = new ObservableCollection<string> { "Модель", "Цвет", "КПП", "Год", "Полная стоимость (у.е.)" };
                var soldIds = db.Orders.Select(o => o.ModelId).Distinct().ToHashSet();
                var data = db.Models
                    .Include(m => m.PriceList)
                    .ToList()
                    .Where(m => !soldIds.Contains(m.ModelId))
                    .OrderBy(m => m.ModelName)
                    .Select(m => new
                    {
                        m.ModelName,
                        m.Color,
                        m.Transmission,
                        YearOfManufacture = (int?)m.PriceList?.YearOfManufacture,
                        TotalCost = m.PriceList == null
                            ? (decimal?)null
                            : m.PriceList.Price + m.PriceList.PrepCost + m.PriceList.TransportCost
                    });
                Rows = ToRows(data, r =>
                {
                    var name = (string)r.ModelName;
                    var color = (string)r.Color;
                    var transmission = (string)r.Transmission;
                    var year = r.YearOfManufacture?.ToString() ?? "-";
                    var total = Money((decimal?)r.TotalCost);
                    return new string?[] { name, color, transmission, year, total };
                });
                break;
            }
            default: // Доходность по моделям
            {
                Headers = new ObservableCollection<string>
                {
                    "Модель", "Цена (у.е.)", "Подготовка (у.е.)", "Транспорт (у.е.)",
                    "Полная стоимость (у.е.)", "Продано", "Выручка (у.е.)"
                };
                var data = db.Models
                    .Include(m => m.PriceList)
                    .OrderBy(m => m.ModelName)
                    .ToList()
                    .Where(m => m.PriceList != null)
                    .Select(m => new
                    {
                        m.ModelName,
                        m.PriceList!.Price,
                        m.PriceList!.PrepCost,
                        m.PriceList!.TransportCost,
                        TotalCost = m.PriceList.Price + m.PriceList.PrepCost + m.PriceList.TransportCost,
                        SoldCount = db.Orders.Count(o => o.ModelId == m.ModelId)
                    });
                Rows = ToRows(data, r =>
                {
                    var name = (string)r.ModelName;
                    var price = Money((decimal)r.Price);
                    var prep = Money((decimal)r.PrepCost);
                    var transport = Money((decimal)r.TransportCost);
                    var total = Money((decimal)r.TotalCost);
                    var soldCount = (int)r.SoldCount;
                    var totalCost = (decimal)r.TotalCost;
                    var revenue = Money(totalCost * soldCount);
                    return new string?[] { name, price, prep, transport, total, soldCount.ToString(), revenue };
                });
                break;
            }
        }
    }

    private static string Money(decimal? value)
        => value?.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) ?? "-";

    private static ObservableCollection<ReportRow> ToRows(IEnumerable<object> data, Func<dynamic, string?[]> select)
    {
        var rows = new List<ReportRow>();
        foreach (var d in data)
        {
            string?[] rawCells = select((dynamic)d);
            var cells = rawCells.Select(c => c ?? "-").ToArray();
            rows.Add(new ReportRow { Cells = cells! });
        }
        return new ObservableCollection<ReportRow>(rows);
    }
}
