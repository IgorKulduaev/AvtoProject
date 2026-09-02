using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AutoSalesApp.Data;
using AutoSalesApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;

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
        using var connection = Database.GetConnection();
        connection.Open();

        switch (SelectedReportIndex)
        {
            case 0: // Отчёт 1. Сводка по поставщикам
            {
                Headers = new ObservableCollection<string>
                {
                    "Название фирмы", "Телефон", "Email", "Моделей предлагает", "Продано автомобилей"
                };
                var data = connection.Query(@"
                    SELECT p.CompanyName, p.Phone, p.Email,
                           COUNT(DISTINCT of.ModelId) AS ModelsCount,
                           COUNT(o.OrderId) AS SoldCount
                    FROM Producer p
                    LEFT JOIN Offer of ON of.ProducerId = p.ProducerId
                    LEFT JOIN [Order] o ON o.ModelId = of.ModelId
                    GROUP BY p.ProducerId
                    ORDER BY p.CompanyName");
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
            case 1: // Отчёт 2. Сводка по моделям
            {
                Headers = new ObservableCollection<string>
                {
                    "Модель", "Цвет", "Обивка", "Мощность", "КПП", "Год", "Цена (у.е.)",
                    "Полная стоимость (у.е.)", "Продаж"
                };
                var data = connection.Query(@"
                    SELECT m.ModelName, m.Color, m.Upholstery, m.MotorPower, m.Transmission,
                           p.YearOfManufacture, p.Price,
                           (p.Price + p.PrepCost + p.TransportCost) AS TotalCost,
                           (SELECT COUNT(*) FROM [Order] o WHERE o.ModelId = m.ModelId) AS SoldCount
                    FROM Model m
                    LEFT JOIN PriceList p ON p.ModelId = m.ModelId
                    ORDER BY m.ModelName");
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
            case 2: // Отчёт 3. Сводка по клиентам
            {
                Headers = new ObservableCollection<string>
                {
                    "Ф.И.О.", "Телефон", "Адрес", "Дата покупки", "Купленная модель", "Стоимость покупки (у.е.)"
                };
                var data = connection.Query(@"
                    SELECT c.FIO, c.Phone, c.Address,
                           strftime('%d.%m.%Y', o.OrderDate) AS OrderDateStr,
                           m.ModelName, o.TotalCost
                    FROM Client c
                    LEFT JOIN [Order] o ON o.ClientId = c.ClientId
                    LEFT JOIN Model m ON m.ModelId = o.ModelId
                    ORDER BY c.FIO, o.OrderDate");
                Rows = ToRows(data, r =>
                {
                    var fio = (string)r.FIO;
                    var phone = (string)r.Phone;
                    var address = (string)r.Address;
                    var date = (string?)r.OrderDateStr ?? "-";
                    var model = (string?)r.ModelName ?? "-";
                    var cost = r.TotalCost == null ? "-" : Money((decimal)r.TotalCost);
                    return new string?[] { fio, phone, address, date, model, cost };
                });
                break;
            }
            case 3: // Отчёт 4. Непроданные модели
            {
                Headers = new ObservableCollection<string> { "Модель", "Цвет", "КПП", "Год", "Полная стоимость (у.е.)" };
                var data = connection.Query(@"
                    SELECT m.ModelName, m.Color, m.Transmission, p.YearOfManufacture,
                           (p.Price + p.PrepCost + p.TransportCost) AS TotalCost
                    FROM Model m
                    LEFT JOIN PriceList p ON p.ModelId = m.ModelId
                    WHERE NOT EXISTS (SELECT 1 FROM [Order] o WHERE o.ModelId = m.ModelId)
                    ORDER BY m.ModelName");
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
            default: // Отчёт 5. Доходность по моделям
            {
                Headers = new ObservableCollection<string>
                {
                    "Модель", "Цена (у.е.)", "Подготовка (у.е.)", "Транспорт (у.е.)",
                    "Полная стоимость (у.е.)", "Продано", "Выручка (у.е.)"
                };
                var data = connection.Query(@"
                    SELECT m.ModelName, p.Price, p.PrepCost, p.TransportCost,
                           (p.Price + p.PrepCost + p.TransportCost) AS TotalCost,
                           (SELECT COUNT(*) FROM [Order] o WHERE o.ModelId = m.ModelId) AS SoldCount
                    FROM Model m
                    JOIN PriceList p ON p.ModelId = m.ModelId
                    ORDER BY m.ModelName");
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
