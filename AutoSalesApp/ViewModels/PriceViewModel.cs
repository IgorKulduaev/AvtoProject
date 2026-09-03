using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoSalesApp.Data;
using AutoSalesApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
namespace AutoSalesApp.ViewModels;

public partial class PriceViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<PriceListItem> _prices = new();

    [ObservableProperty]
    private ObservableCollection<Model> _models = new();

    [ObservableProperty]
    private PriceListItem? _selectedPrice;

    [ObservableProperty]
    private Model? _selectedModel;

    [ObservableProperty]
    private string _yearOfManufacture = DateTime.Now.Year.ToString();

    [ObservableProperty]
    private string _price = string.Empty;

    [ObservableProperty]
    private string _prepCost = string.Empty;

    [ObservableProperty]
    private string _transportCost = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public PriceViewModel()
    {
        AddCommand = new RelayCommand(Add);
        DeleteCommand = new RelayCommand(Delete, () => SelectedPrice != null);
        RefreshCommand = new RelayCommand(Load);
        Load();
        LoadModels();
    }

    private void Load()
    {
        using var db = new AppDbContext();
        var prices = db.PriceLists.Include(p => p.Model).ToList();
        var rows = prices.Select(p => new PriceListItem
        {
            PriceId = p.PriceId,
            ModelId = p.ModelId,
            ModelName = p.Model?.ModelName ?? string.Empty,
            YearOfManufacture = p.YearOfManufacture,
            Price = p.Price,
            PrepCost = p.PrepCost,
            TransportCost = p.TransportCost,
            TotalCost = p.Price + p.PrepCost + p.TransportCost
        }).OrderBy(i => i.PriceId).ToList();
        Prices = new ObservableCollection<PriceListItem>(rows);
    }

    private void LoadModels()
    {
        using var db = new AppDbContext();
        var list = db.Models.OrderBy(m => m.ModelName).ToList();
        Models = new ObservableCollection<Model>(list);
    }

    private void Add()
    {
        ErrorMessage = string.Empty;

        if (SelectedModel == null)
        {
            ErrorMessage = "Выберите модель";
            return;
        }
        if (!TryParseMoney(Price, out decimal price) || price <= 0)
        {
            ErrorMessage = "Цена: введите положительное число";
            return;
        }
        if (!TryParseMoney(PrepCost, out decimal prepCost) || !TryParseMoney(TransportCost, out decimal transportCost)
            || prepCost < 0 || transportCost < 0)
        {
            ErrorMessage = "Подготовка и транспорт: введите неотрицательные числа";
            return;
        }
        if (!int.TryParse(YearOfManufacture, out int year) || year < 1950 || year > DateTime.Now.Year + 1)
        {
            ErrorMessage = "Год выпуска: введите корректный год (1950 – {0}".Replace("{0}", (DateTime.Now.Year + 1).ToString()) + ")";
            return;
        }

        using var db = new AppDbContext();
        var exists = db.PriceLists.Any(p => p.ModelId == SelectedModel.ModelId);
        if (exists)
        {
            ErrorMessage = "Для этой модели уже есть прейскурант (связь 1:1)";
            return;
        }

        db.PriceLists.Add(new PriceList
        {
            ModelId = SelectedModel.ModelId,
            YearOfManufacture = year,
            Price = price,
            PrepCost = prepCost,
            TransportCost = transportCost
        });
        db.SaveChanges();

        Price = PrepCost = TransportCost = string.Empty;
        YearOfManufacture = DateTime.Now.Year.ToString();
        Load();
    }

    private static bool TryParseMoney(string? text, out decimal value)
    {
        text = (text ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private void Delete()
    {
        if (SelectedPrice == null) return;

        using var db = new AppDbContext();
        var item = db.PriceLists.FirstOrDefault(p => p.PriceId == SelectedPrice.PriceId);
        if (item != null)
        {
            db.PriceLists.Remove(item);
            db.SaveChanges();
        }
        Load();
    }

    partial void OnSelectedPriceChanged(PriceListItem? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}
