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
    private int _doorCount = 4;

    [ObservableProperty]
    private string _transmission = "автоматическая";

    [ObservableProperty]
    private int _yearOfManufacture = 2025;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private decimal _prepCost;

    [ObservableProperty]
    private decimal _transportCost;

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
            return;

        using var connection = Database.GetConnection();
        connection.Open();

        // Добавляем модель
        connection.Execute(@"
            INSERT INTO Model (ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount, Transmission)
            VALUES (@ModelCode, @ModelName, @Color, @Upholstery, @MotorPower, @DoorCount, @Transmission)",
            new { ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount, Transmission });

        // Получаем ID новой модели
        var modelId = connection.ExecuteScalar<int>("SELECT last_insert_rowid()");

        // Добавляем цену
        connection.Execute(@"
            INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost)
            VALUES (@ModelId, @YearOfManufacture, @Price, @PrepCost, @TransportCost)",
            new { ModelId = modelId, YearOfManufacture, Price, PrepCost, TransportCost });

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
        DoorCount = 4;
        Transmission = "автоматическая";
        YearOfManufacture = 2025;
        Price = PrepCost = TransportCost = 0;
        SelectedProducers.Clear();

        LoadModels();
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