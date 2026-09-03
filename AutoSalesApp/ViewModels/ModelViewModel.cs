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

public partial class ModelViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Model> _models = new();

    [ObservableProperty]
    private ObservableCollection<Producer> _producers = new();

    [ObservableProperty]
    private ObservableCollection<Producer> _selectedProducers = new();

    [ObservableProperty]
    private Model? _selectedModel;

    [ObservableProperty]
    private string _modelCode = string.Empty;

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private string _upholstery = string.Empty;

    [ObservableProperty]
    private string _motorPower = string.Empty;

    [ObservableProperty]
    private string _doorCount = "4";

    [ObservableProperty]
    private string _transmission = "автоматическая";

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

    public ModelViewModel()
    {
        AddCommand = new RelayCommand(Add);
        DeleteCommand = new RelayCommand(Delete, () => SelectedModel != null);
        RefreshCommand = new RelayCommand(Load);
        Load();
        LoadProducers();
    }

    private void Load()
    {
        using var db = new AppDbContext();
        var list = db.Models.OrderBy(m => m.ModelId).ToList();
        Models = new ObservableCollection<Model>(list);
    }

    private void LoadProducers()
    {
        using var db = new AppDbContext();
        var list = db.Producers.OrderBy(p => p.CompanyName).ToList();
        Producers = new ObservableCollection<Producer>(list);
    }

    private void Add()
    {
        if (string.IsNullOrWhiteSpace(ModelCode) || string.IsNullOrWhiteSpace(ModelName))
        {
            ErrorMessage = "Заполните код и название модели";
            return;
        }

        if (!int.TryParse(DoorCount, out int doorCount) || doorCount <= 0)
        {
            ErrorMessage = "Двери: введите целое положительное число";
            return;
        }

        if (!int.TryParse(YearOfManufacture, out int year) || year < 1950 || year > DateTime.Now.Year + 1)
        {
            ErrorMessage = "Год: введите корректный год выпуска";
            return;
        }

        if (!TryParseMoney(Price, out decimal price) || price <= 0)
        {
            ErrorMessage = "Цена: введите положительное число";
            return;
        }

        if (!TryParseMoney(PrepCost, out decimal prepCost) || prepCost < 0 ||
            !TryParseMoney(TransportCost, out decimal transportCost) || transportCost < 0)
        {
            ErrorMessage = "Подготовка и транспорт: введите неотрицательные числа";
            return;
        }

        ErrorMessage = string.Empty;

        using var db = new AppDbContext();
        var model = new Model
        {
            ModelCode = ModelCode,
            ModelName = ModelName,
            Color = Color,
            Upholstery = Upholstery,
            MotorPower = MotorPower,
            DoorCount = doorCount,
            Transmission = Transmission
        };
        db.Models.Add(model);
        db.SaveChanges();

        db.PriceLists.Add(new PriceList
        {
            ModelId = model.ModelId,
            YearOfManufacture = year,
            Price = price,
            PrepCost = prepCost,
            TransportCost = transportCost
        });
        db.SaveChanges();

        foreach (var producer in SelectedProducers)
            db.Offers.Add(new Offer { ProducerId = producer.ProducerId, ModelId = model.ModelId });
        db.SaveChanges();

        ModelCode = ModelName = Color = Upholstery = MotorPower = string.Empty;
        DoorCount = "4";
        Transmission = "автоматическая";
        YearOfManufacture = DateTime.Now.Year.ToString();
        Price = PrepCost = TransportCost = string.Empty;
        SelectedProducers.Clear();

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
        if (SelectedModel == null) return;

        using var db = new AppDbContext();
        var offers = db.Offers.Where(o => o.ModelId == SelectedModel.ModelId).ToList();
        db.Offers.RemoveRange(offers);

        var prices = db.PriceLists.Where(p => p.ModelId == SelectedModel.ModelId).ToList();
        db.PriceLists.RemoveRange(prices);

        db.Models.Remove(SelectedModel);
        db.SaveChanges();
        Load();
    }

    partial void OnSelectedModelChanged(Model? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}
