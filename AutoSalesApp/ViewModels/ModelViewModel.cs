using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoSalesApp.Models;
using AutoSalesApp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;

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
    private string _yearOfManufacture = "2025";

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
        AddCommand = new RelayCommand(AddModel);
        DeleteCommand = new RelayCommand(DeleteModel, () => SelectedModel != null);
        RefreshCommand = new RelayCommand(LoadModels);
        LoadModels();
        LoadProducers();
    }

    private void LoadModels()
    {
        using var connection = Database.GetConnection();
        connection.Open();
        var list = connection.Query<Model>("SELECT * FROM Model ORDER BY ModelId");
        Models = new ObservableCollection<Model>(list);
    }

    private void LoadProducers()
    {
        using var connection = Database.GetConnection();
        connection.Open();
        var list = connection.Query<Producer>("SELECT * FROM Producer ORDER BY CompanyName");
        Producers = new ObservableCollection<Producer>(list);
    }

    private void AddModel()
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

        if (!TryParseMoney(PrepCost, out decimal prepCost) || prepCost < 0)
        {
            ErrorMessage = "Подготовка: введите неотрицательное число";
            return;
        }

        if (!TryParseMoney(TransportCost, out decimal transportCost) || transportCost < 0)
        {
            ErrorMessage = "Транспорт: введите неотрицательное число";
            return;
        }

        ErrorMessage = string.Empty;

        using var connection = Database.GetConnection();
        connection.Open();

        // Добавляем модель
        connection.Execute(@"
            INSERT INTO Model (ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount, Transmission)
            VALUES (@ModelCode, @ModelName, @Color, @Upholstery, @MotorPower, @DoorCount, @Transmission)",
            new { ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount = doorCount, Transmission });

        // Получаем ID новой модели
        var modelId = connection.ExecuteScalar<int>("SELECT last_insert_rowid()");

        // Добавляем цену
        connection.Execute(@"
            INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost)
            VALUES (@ModelId, @YearOfManufacture, @Price, @PrepCost, @TransportCost)",
            new { ModelId = modelId, YearOfManufacture = year, Price = price, PrepCost = prepCost, TransportCost = transportCost });

        // Добавляем связи с поставщиками
        foreach (var producer in SelectedProducers)
        {
            connection.Execute(@"
                INSERT INTO Offer (ProducerId, ModelId)
                VALUES (@ProducerId, @ModelId)",
                new { ProducerId = producer.ProducerId, ModelId = modelId });
        }

        // Очищаем поля
        ModelCode = ModelName = Color = Upholstery = MotorPower = string.Empty;
        DoorCount = "4";
        Transmission = "автоматическая";
        YearOfManufacture = DateTime.Now.Year.ToString();
        Price = PrepCost = TransportCost = string.Empty;
        SelectedProducers.Clear();

        LoadModels();
    }

    private static bool TryParseMoney(string? text, out decimal value)
    {
        text = (text ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private void DeleteModel()
    {
        if (SelectedModel == null) return;

        using var connection = Database.GetConnection();
        connection.Open();

        // Удаляем связи
        connection.Execute("DELETE FROM Offer WHERE ModelId = @Id", new { Id = SelectedModel.ModelId });
        // Удаляем цену
        connection.Execute("DELETE FROM PriceList WHERE ModelId = @Id", new { Id = SelectedModel.ModelId });
        // Удаляем модель
        connection.Execute("DELETE FROM Model WHERE ModelId = @Id", new { Id = SelectedModel.ModelId });

        LoadModels();
    }

    partial void OnSelectedModelChanged(Model? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}