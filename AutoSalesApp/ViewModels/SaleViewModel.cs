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

public partial class SaleViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<OrderItem> _orders = new();

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private ObservableCollection<ModelWithPrice> _models = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private ModelWithPrice? _selectedModel;

    [ObservableProperty]
    private DateTimeOffset? _orderDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _orderNumber = string.Empty;

    [ObservableProperty]
    private decimal _totalCost;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private OrderItem? _selectedOrder;

    public ICommand SellCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }

    public SaleViewModel()
    {
        SellCommand = new RelayCommand(Sell);
        RefreshCommand = new RelayCommand(Load);
        DeleteCommand = new RelayCommand(Delete, () => SelectedOrder != null);
        Load();
        LoadClients();
        LoadModels();
    }

    private void Delete()
    {
        if (SelectedOrder == null) return;

        using var db = new AppDbContext();
        var order = db.Orders.FirstOrDefault(o => o.OrderId == SelectedOrder.OrderId);
        if (order != null)
        {
            db.Orders.Remove(order);
            db.SaveChanges();
        }
        Load();
    }

    partial void OnSelectedOrderChanged(OrderItem? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }

    private void Load()
    {
        using var db = new AppDbContext();
        var orders = db.Orders
            .Include(o => o.Client)
            .Include(o => o.Model)
            .OrderByDescending(o => o.OrderId)
            .ToList();

        var items = orders.Select(o => new OrderItem
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            ClientFio = o.Client?.FIO ?? string.Empty,
            ModelName = o.Model?.ModelName ?? string.Empty,
            OrderDate = o.OrderDate,
            TotalCost = o.TotalCost
        }).ToList();

        Orders = new ObservableCollection<OrderItem>(items);
    }

    private void LoadClients()
    {
        using var db = new AppDbContext();
        var list = db.Clients.OrderBy(c => c.FIO).ToList();
        Clients = new ObservableCollection<Client>(list);
    }

    private void LoadModels()
    {
        using var db = new AppDbContext();
        var rows = db.PriceLists
            .Include(p => p.Model)
            .ToList()
            .Select(p => new ModelWithPrice
            {
                ModelId = p.ModelId,
                ModelName = p.Model!.ModelName,
                Color = p.Model!.Color,
                Transmission = p.Model!.Transmission,
                YearOfManufacture = p.YearOfManufacture,
                TotalCost = p.Price + p.PrepCost + p.TransportCost
            })
            .ToList();
        Models = new ObservableCollection<ModelWithPrice>(rows);
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
        if (OrderDate == null)
        {
            ErrorMessage = "Укажите дату заказа";
            return;
        }

        using var db = new AppDbContext();

        int num;
        var lastNumber = db.Orders
            .OrderByDescending(o => o.OrderId)
            .Select(o => o.OrderNumber)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(lastNumber) || !int.TryParse(lastNumber.TrimStart('Д', 'д'), out num))
            num = db.Orders.Count();

        var orderNumber = "Д" + (num + 1).ToString("D3");

        db.Orders.Add(new Order
        {
            OrderNumber = orderNumber,
            ClientId = SelectedClient.ClientId,
            ModelId = SelectedModel.ModelId,
            OrderDate = OrderDate.Value.Date,
            TotalCost = TotalCost
        });
        db.SaveChanges();

        SelectedModel = null;
        SelectedClient = null;
        OrderNumber = string.Empty;
        TotalCost = 0;
        Load();
    }

    partial void OnSelectedModelChanged(ModelWithPrice? value)
    {
        if (value != null)
            TotalCost = value.TotalCost;
    }
}
