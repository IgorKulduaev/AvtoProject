using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoSalesApp.Models;
using AutoSalesApp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;

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
        using var connection = Database.GetConnection();
        connection.Open();
        var list = connection.Query<Client>("SELECT * FROM Client ORDER BY ClientId");
        Clients = new ObservableCollection<Client>(list);
    }

    private void AddClient()
    {
        if (string.IsNullOrWhiteSpace(Fio) || string.IsNullOrWhiteSpace(Phone))
            return;

        using var connection = Database.GetConnection();
        connection.Open();
        connection.Execute(@"
            INSERT INTO Client (FIO, Phone, Address)
            VALUES (@Fio, @Phone, @Address)",
            new { Fio, Phone, Address });

        Fio = Phone = Address = string.Empty;
        LoadClients();
    }

    private void DeleteClient()
    {
        if (SelectedClient == null) return;

        using var connection = Database.GetConnection();
        connection.Open();
        connection.Execute("DELETE FROM Client WHERE ClientId = @Id", new { Id = SelectedClient.ClientId });
        LoadClients();
    }

    partial void OnSelectedClientChanged(Client? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}