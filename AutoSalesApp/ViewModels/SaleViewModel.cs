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

public partial class SaleViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private ObservableCollection<ModelWithPrice> _models = new();

    [ObservableProperty]
    private ObservableCollection<OrderItem> _orders = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private ModelWithPrice? _selectedModel;

    [ObservableProperty]
    private OrderItem? _selectedOrder;

    [ObservableProperty]
    private DateTimeOffset _orderDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _orderNumber = string.Empty;

    [ObservableProperty]
    private decimal _totalCost;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ICommand SellCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public SaleViewModel()
    {
        SellCommand = new RelayCommand(Sell);
        DeleteCommand = new RelayCommand(DeleteOrder, () => SelectedOrder != null);
        RefreshCommand = new RelayCommand(LoadAll);
        LoadAll();
        OrderNumber = NextOrderNumber();
    }

    private void LoadAll()
    {
        using var connection = Database.GetConnection();
        connection.Open();

        Clients = new ObservableCollection<Client>(
            connection.Query<Client>("SELECT * FROM Client ORDER BY FIO").ToList());

        Models = new ObservableCollection<ModelWithPrice>(
            connection.Query<ModelWithPrice>(@"
                SELECT m.ModelId, m.ModelName, m.Color, m.Transmission,
                       p.YearOfManufacture,
                       (p.Price + p.PrepCost + p.TransportCost) AS TotalCost
                FROM Model m
                JOIN PriceList p ON p.ModelId = m.ModelId
                ORDER BY m.ModelName").ToList());

        Orders = new ObservableCollection<OrderItem>(
            connection.Query<OrderItem>(@"
                SELECT o.OrderId, o.OrderNumber, c.FIO AS ClientFio, m.ModelName,
                       o.OrderDate, o.TotalCost
                FROM [Order] o
                JOIN Client c ON c.ClientId = o.ClientId
                JOIN Model m ON m.ModelId = o.ModelId
                ORDER BY o.OrderId").ToList());
    }

    private string NextOrderNumber()
    {
        using var connection = Database.GetConnection();
        connection.Open();
        var max = connection.ExecuteScalar<int?>(
            "SELECT MAX(CAST(SUBSTR(OrderNumber, 2) AS INTEGER)) FROM [Order] WHERE OrderNumber GLOB 'Д[0-9]*'");
        return $"Д{(max ?? 0) + 1:000}";
    }

    private void Sell()
    {
        ErrorMessage = string.Empty;

        if (SelectedClient == null)
        {
            ErrorMessage = "Выберите клиента";
            return;
        }
        if (SelectedModel == null)
        {
            ErrorMessage = "Выберите модель";
            return;
        }
        if (string.IsNullOrWhiteSpace(OrderNumber))
        {
            ErrorMessage = "Введите номер договора";
            return;
        }

        using var connection = Database.GetConnection();
        connection.Open();

        var duplicate = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM [Order] WHERE OrderNumber = @OrderNumber",
            new { OrderNumber });
        if (duplicate > 0)
        {
            ErrorMessage = "Договор с таким номером уже существует";
            return;
        }

        connection.Execute(@"
            INSERT INTO [Order] (OrderNumber, ClientId, ModelId, OrderDate, TotalCost)
            VALUES (@OrderNumber, @ClientId, @ModelId, @OrderDate, @TotalCost)",
            new
            {
                OrderNumber,
                ClientId = SelectedClient.ClientId,
                ModelId = SelectedModel.ModelId,
                OrderDate = OrderDate.DateTime,
                TotalCost
            });

        LoadAll();
        OrderNumber = NextOrderNumber();
        OrderDate = DateTimeOffset.Now;
    }

    private void DeleteOrder()
    {
        if (SelectedOrder == null) return;

        using var connection = Database.GetConnection();
        connection.Open();
        connection.Execute("DELETE FROM [Order] WHERE OrderId = @Id", new { Id = SelectedOrder.OrderId });
        LoadAll();
        OrderNumber = NextOrderNumber();
    }

    partial void OnSelectedModelChanged(ModelWithPrice? value)
    {
        TotalCost = value?.TotalCost ?? 0;
    }

    partial void OnSelectedOrderChanged(OrderItem? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}
