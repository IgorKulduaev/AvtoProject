using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoSalesApp.Data;
using AutoSalesApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;

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
        AddCommand = new RelayCommand(AddPrice);
        DeleteCommand = new RelayCommand(DeletePrice, () => SelectedPrice != null);
        RefreshCommand = new RelayCommand(LoadPrices);
        LoadPrices();
        LoadModels();
    }

    private void LoadPrices()
    {
        using var connection = Database.GetConnection();
        connection.Open();
        var list = connection.Query<PriceListItem>(@"
            SELECT p.PriceId, p.ModelId, m.ModelName, p.YearOfManufacture,
                   p.Price, p.PrepCost, p.TransportCost,
                   (p.Price + p.PrepCost + p.TransportCost) AS TotalCost
            FROM PriceList p
            JOIN Model m ON m.ModelId = p.ModelId
            ORDER BY p.PriceId").ToList();
        Prices = new ObservableCollection<PriceListItem>(list);
    }

    private void LoadModels()
    {
        using var connection = Database.GetConnection();
        connection.Open();
        var list = connection.Query<Model>("SELECT * FROM Model ORDER BY ModelName").ToList();
        Models = new ObservableCollection<Model>(list);
    }

    private void AddPrice()
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
            ErrorMessage = "Год выпуска: введите корректный год (1950 – " + (DateTime.Now.Year + 1) + ")";
            return;
        }

        using var connection = Database.GetConnection();
        connection.Open();

        var exists = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM PriceList WHERE ModelId = @ModelId",
            new { SelectedModel.ModelId });
        if (exists > 0)
        {
            ErrorMessage = "Для этой модели уже есть прейскурант (связь 1:1)";
            return;
        }

        connection.Execute(@"
            INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost)
            VALUES (@ModelId, @YearOfManufacture, @Price, @PrepCost, @TransportCost)",
            new { SelectedModel.ModelId, YearOfManufacture = year, Price = price, PrepCost = prepCost, TransportCost = transportCost });

        Price = PrepCost = TransportCost = string.Empty;
        YearOfManufacture = DateTime.Now.Year.ToString();
        LoadPrices();
    }

    private static bool TryParseMoney(string? text, out decimal value)
    {
        text = (text ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private void DeletePrice()
    {
        if (SelectedPrice == null) return;

        using var connection = Database.GetConnection();
        connection.Open();
        connection.Execute("DELETE FROM PriceList WHERE PriceId = @Id", new { Id = SelectedPrice.PriceId });
        LoadPrices();
    }

    partial void OnSelectedPriceChanged(PriceListItem? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}
