using AutoSalesApp.Models;
using AutoSalesApp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
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
        AddCommand = new RelayCommand(Add);
        DeleteCommand = new RelayCommand(Delete, () => SelectedProducer != null);
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    private void Load()
    {
        using var db = new AppDbContext();
        var list = db.Producers.OrderBy(p => p.CompanyName).ToList();
        Producers = new ObservableCollection<Producer>(list);
    }

    private void Add()
    {
        if (string.IsNullOrWhiteSpace(CompanyName)) return;

        using var db = new AppDbContext();
        db.Producers.Add(new Producer
        {
            CompanyCode = CompanyCode,
            CompanyName = CompanyName,
            Phone = Phone,
            Email = Email,
            Website = Website
        });
        db.SaveChanges();

        CompanyCode = CompanyName = Phone = Email = Website = string.Empty;
        Load();
    }

    private void Delete()
    {
        if (SelectedProducer == null) return;

        using var db = new AppDbContext();
        db.Producers.Remove(SelectedProducer);
        db.SaveChanges();
        Load();
    }

    partial void OnSelectedProducerChanged(Producer? value)
    {
        ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
    }
}
