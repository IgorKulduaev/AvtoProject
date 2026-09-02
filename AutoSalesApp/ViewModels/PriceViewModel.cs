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
    private int _yearOfManufacture = DateTime.Now.Year;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private decimal _prepCost;

    [ObservableProperty]
    private decimal _transportCost;

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
        if (Price <= 0)
        {
            ErrorMessage = "Цена должна быть больше 0";
            return;
        }
        if (PrepCost < 0 || TransportCost < 0)
        {
            ErrorMessage = "Предпродажная подготовка и транспортные издержки не могут быть отрицательными";
            return;
        }
        if (YearOfManufacture > DateTime.Now.Year + 1)
        {
            ErrorMessage = $"Год выпуска не может быть больше {DateTime.Now.Year + 1}";
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
            new { SelectedModel.ModelId, YearOfManufacture, Price, PrepCost, TransportCost });

        Price = PrepCost = TransportCost = 0;
        YearOfManufacture = DateTime.Now.Year;
        LoadPrices();
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
