using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoSalesApp.Data;
using AutoSalesApp.Models;
using AutoSalesApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace AutoSalesApp.ViewModels;

public partial class ChartViewModel : ObservableObject
{
    // График 1: количество проданных автомобилей по моделям
    public ISeries[] ModelSalesSeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] ModelSalesXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] ModelSalesYAxes { get; private set; } = Array.Empty<Axis>();

    // График 2: доля продаж по поставщикам (круговая)
    public ISeries[] ProducerShareSeries { get; private set; } = Array.Empty<ISeries>();

    // График 3: динамика продаж по месяцам (линейный)
    public ISeries[] MonthlySalesSeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] MonthlySalesXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] MonthlySalesYAxes { get; private set; } = Array.Empty<Axis>();

    // График 4: распределение цен на модели (гистограмма)
    public ISeries[] PriceHistogramSeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] PriceHistogramXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] PriceHistogramYAxes { get; private set; } = Array.Empty<Axis>();

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<string> SavePngCommand { get; }

    private List<(string Model, double Count)> _modelSales = new();
    private List<(string Producer, double Count)> _producerShare = new();
    private List<(string Month, double Count)> _monthlySales = new();
    private List<(string Range, double Count)> _priceHistogram = new();

    public ChartViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        SavePngCommand = new AsyncRelayCommand<string>(SavePngAsync);
        LoadData();
    }

    private async Task LoadDataAsync()
    {
        LoadData();
        await Task.CompletedTask;
    }

    private void LoadData()
    {
        using var db = new AppDbContext();

        // 1. Продажи по моделям
        _modelSales = db.Models
            .Include(m => m.Orders)
            .OrderBy(m => m.ModelName)
            .ToList()
            .Select(m => (Model: m.ModelName, Count: (double)m.Orders.Count))
            .ToList();

        // 2. Доля продаж по поставщикам
        _producerShare = db.Producers
            .Include(p => p.Offers)
            .ThenInclude(o => o.Model!)
            .ThenInclude(m => m.Orders)
            .OrderBy(p => p.CompanyName)
            .ToList()
            .Select(p => (Producer: p.CompanyName,
                Count: (double)p.Offers.SelectMany(o => o.Model!.Orders).Count()))
            .ToList();

        // 3. Динамика продаж по месяцам
        var raw = db.Orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { Key = $"{g.Key.Year}-{g.Key.Month:D2}", Count = g.Count() })
            .ToList()
            .OrderBy(x => x.Key)
            .Select(x => (Month: x.Key, Count: (double)x.Count))
            .ToList();
        _monthlySales = FillMonthGaps(raw);

        // 4. Распределение цен (гистограмма)
        var prices = db.PriceLists
            .Select(p => p.Price + p.PrepCost + p.TransportCost)
            .ToList();
        _priceHistogram = BuildHistogram(prices, 5);

        BuildSeries();
    }

    private void BuildSeries()
    {
        // Столбчатая
        ModelSalesSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Продано",
                Values = _modelSales.Select(x => x.Count).ToArray()
            }
        };
        ModelSalesXAxes = new Axis[] { new Axis { Labels = _modelSales.Select(x => x.Model).ToArray() } };
        ModelSalesYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Количество",
                MinLimit = 0
            }
        };

        // Круговая
        ProducerShareSeries = _producerShare
            .Select(x => (ISeries)new PieSeries<double>
            {
                Name = x.Producer,
                Values = new double[] { x.Count }
            })
            .ToArray();

        // Линейный
        MonthlySalesSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Продажи",
                Values = _monthlySales.Select(x => x.Count).ToArray()
            }
        };
        MonthlySalesXAxes = new Axis[] { new Axis { Labels = _monthlySales.Select(x => x.Month).ToArray() } };
        MonthlySalesYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Продано",
                MinLimit = 0
            }
        };

        // Гистограмма
        PriceHistogramSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Моделей",
                Values = _priceHistogram.Select(x => x.Count).ToArray()
            }
        };
        PriceHistogramXAxes = new Axis[] { new Axis { Labels = _priceHistogram.Select(x => x.Range).ToArray() } };
        PriceHistogramYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Количество моделей",
                MinLimit = 0
            }
        };
    }

    private static List<(string Month, double Count)> FillMonthGaps(List<(string Month, double Count)> raw)
    {
        if (raw.Count == 0) return raw;

        var result = new List<(string, double)>();
        var start = ParseMonth(raw.First().Month);
        var end = ParseMonth(raw.Last().Month);
        var map = raw.ToDictionary(x => ParseMonth(x.Month), x => x.Count);

        for (var m = start; m <= end; m = m.AddMonths(1))
        {
            result.Add(($"{m.Year}-{m.Month:D2}", map.GetValueOrDefault(m, 0d)));
        }
        return result;
    }

    private static DateTime ParseMonth(string month)
    {
        var parts = month.Split('-');
        return new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1);
    }

    private static List<(string Range, double Count)> BuildHistogram(List<decimal> prices, int bins)
    {
        var result = new List<(string, double)>();
        if (prices.Count == 0) return result;

        var min = (double)prices.Min();
        var max = (double)prices.Max();
        var step = (max - min) / bins;
        if (step <= 0) step = 1;

        for (int i = 0; i < bins; i++)
        {
            var lo = min + i * step;
            var hi = i == bins - 1 ? max + 0.01 : lo + step;
            var count = prices.Count(p => (double)p >= lo && (double)p < hi);
            result.Add(($"{(int)lo / 1000}-{(int)hi / 1000} тыс.", count));
        }
        return result;
    }

    private async Task SavePngAsync(string? chartName)
    {
        var path = await FileDialogHelper.PickSaveFileAsync("График PNG", "png");
        if (path == null) return;

        switch (chartName)
        {
            case "models":
            {
                var chart = new SKCartesianChart { Width = 1200, Height = 700 };
                chart.Series = ModelSalesSeries;
                chart.XAxes = ModelSalesXAxes;
                chart.YAxes = ModelSalesYAxes;
                SaveChart(chart, path);
                break;
            }
            case "producers":
            {
                var chart = new SKPieChart { Width = 1000, Height = 700 };
                chart.Series = ProducerShareSeries;
                SaveChart(chart, path);
                break;
            }
            case "months":
            {
                var chart = new SKCartesianChart { Width = 1200, Height = 700 };
                chart.Series = MonthlySalesSeries;
                chart.XAxes = MonthlySalesXAxes;
                chart.YAxes = MonthlySalesYAxes;
                SaveChart(chart, path);
                break;
            }
            case "prices":
            {
                var chart = new SKCartesianChart { Width = 1200, Height = 700 };
                chart.Series = PriceHistogramSeries;
                chart.XAxes = PriceHistogramXAxes;
                chart.YAxes = PriceHistogramYAxes;
                SaveChart(chart, path);
                break;
            }
        }
    }

    private static void SaveChart(InMemorySkiaSharpChart chart, string path)
    {
        chart.SaveImage(path, SKEncodedImageFormat.Png, 100);
    }
}
