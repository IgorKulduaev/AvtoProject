using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoSalesApp.Models;
using AutoSalesApp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace AutoSalesApp.ViewModels;

public partial class ClientViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _fio = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public ClientViewModel()
    {
        AddCommand = new RelayCommand(AddClient);
        DeleteCommand = new RelayCommand(DeleteClient, () => SelectedClient != null);
        RefreshCommand = new RelayCommand(LoadClients);
        LoadClients();
    }

    private void LoadClients()
    {
        using var db = new AppDbContext();
        var list = db.Clients.OrderBy(c => c.ClientId).ToList();
        Clients = new ObservableCollection<Client>(list);
    }

    private void AddClient()
    {
        if (string.IsNullOrWhiteSpace(Fio) || string.IsNullOrWhiteSpace(Phone))
            return;

        using var db = new AppDbContext();
        db.Clients.Add(new Client { FIO = Fio, Phone = Phone, Address = Address });
        db.SaveChanges();

        Fio = Phone = Address = string.Empty;
        LoadClients();
    }

    private void DeleteClient()
    {
        if (SelectedClient == null) return;

        using var db = new AppDbContext();
        db.Clients.Remove(SelectedClient);
        db.SaveChanges();
        LoadClients();
    }

    partial void OnSelectedClientChanged(Client? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}
