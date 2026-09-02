using AutoSalesApp.Data;
using AutoSalesApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace AutoSalesApp.ViewModels;

public partial class ProducerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Producer> _producers = new();

    [ObservableProperty]
    private Producer? _selectedProducer;

    [ObservableProperty]
    private string _companyCode = string.Empty;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _website = string.Empty;

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public ProducerViewModel()
    {
        AddCommand = new RelayCommand(AddProducer);
        DeleteCommand = new RelayCommand(DeleteProducer, () => SelectedProducer != null);
        RefreshCommand = new RelayCommand(LoadProducers);
        LoadProducers();
    }

    private void LoadProducers()
    {
        using var connection = Database.GetConnection();
        connection.Open();
        var list = connection.Query<Producer>("SELECT * FROM Producer ORDER BY ProducerId").ToList();

        System.Diagnostics.Debug.WriteLine($"Загружено поставщиков: {list.Count}");
        foreach (var p in list)
        {
            System.Diagnostics.Debug.WriteLine($"  {p.ProducerId}: {p.CompanyName}");
        }

        // Создаём новую коллекцию вместо Clear/Add
        Producers = new ObservableCollection<Producer>(list);
    }

    private void AddProducer()
    {
        if (string.IsNullOrWhiteSpace(CompanyCode) || string.IsNullOrWhiteSpace(CompanyName))
            return;

        using var connection = Database.GetConnection();
        connection.Open();
        connection.Execute(@"
            INSERT INTO Producer (CompanyCode, CompanyName, Phone, Email, Website)
            VALUES (@CompanyCode, @CompanyName, @Phone, @Email, @Website)",
            new { CompanyCode, CompanyName, Phone, Email, Website });

        CompanyCode = CompanyName = Phone = Email = Website = string.Empty;
        LoadProducers();
    }

    private void DeleteProducer()
    {
        if (SelectedProducer == null) return;

        using var connection = Database.GetConnection();
        connection.Open();
        connection.Execute("DELETE FROM Producer WHERE ProducerId = @Id", new { Id = SelectedProducer.ProducerId });
        LoadProducers();
    }

    partial void OnSelectedProducerChanged(Producer? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}