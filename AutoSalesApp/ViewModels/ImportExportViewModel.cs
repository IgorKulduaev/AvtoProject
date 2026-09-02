using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoSalesApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoSalesApp.ViewModels;

public partial class ImportExportViewModel : ObservableObject
{
    // Варианты сущностей для CSV
    private static readonly (string Name, string Key)[] CsvImportEntities =
    {
        ("Поставщики", "producers"),
        ("Модели", "models"),
        ("Клиенты", "clients"),
        ("Прейскурант", "pricelist")
    };

    private static readonly (string Name, string Key)[] CsvExportEntities =
    {
        ("Поставщики", "producers"),
        ("Модели", "models"),
        ("Связи (поставщик–модель)", "offers"),
        ("Прейскурант", "pricelist"),
        ("Клиенты", "clients"),
        ("Продажи", "sales")
    };

    private static readonly (string Name, string Key)[] ExcelImportEntities =
    {
        ("Связи (поставщик–модель)", "offers"),
        ("Продажи", "sales")
    };

    public ObservableCollection<string> CsvImportEntityNames { get; } =
        new(Array.ConvertAll(CsvImportEntities, e => e.Name));

    public ObservableCollection<string> CsvExportEntityNames { get; } =
        new(Array.ConvertAll(CsvExportEntities, e => e.Name));

    public ObservableCollection<string> ExcelImportEntityNames { get; } =
        new(Array.ConvertAll(ExcelImportEntities, e => e.Name));

    [ObservableProperty]
    private int _csvImportEntityIndex;

    [ObservableProperty]
    private int _csvExportEntityIndex;

    [ObservableProperty]
    private int _excelImportEntityIndex;

    [ObservableProperty]
    private string _log = "Выберите файл для импорта или экспорта.\n";

    public IAsyncRelayCommand ImportCsvCommand { get; }
    public IAsyncRelayCommand ImportExcelCommand { get; }
    public IAsyncRelayCommand ImportJsonCommand { get; }
    public IAsyncRelayCommand ImportXmlCommand { get; }
    public IAsyncRelayCommand ExportCsvCommand { get; }
    public IAsyncRelayCommand ExportExcelCommand { get; }
    public IAsyncRelayCommand ExportPdfCommand { get; }

    public ImportExportViewModel()
    {
        ImportCsvCommand = new AsyncRelayCommand(ImportCsvAsync);
        ImportExcelCommand = new AsyncRelayCommand(ImportExcelAsync);
        ImportJsonCommand = new AsyncRelayCommand(ImportJsonAsync);
        ImportXmlCommand = new AsyncRelayCommand(ImportXmlAsync);
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
        ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
    }

    private async Task ImportCsvAsync()
    {
        var path = await FileDialogHelper.PickOpenFileAsync("Импорт CSV", "csv");
        if (path == null) return;

        var entity = CsvImportEntities[CsvImportEntityIndex];
        AppendLog($"Импорт CSV ({entity.Name}) из {path}...");
        var result = ImportService.ImportCsv(path, entity.Key);
        AppendResult(result);
    }

    private async Task ImportExcelAsync()
    {
        var path = await FileDialogHelper.PickOpenFileAsync("Импорт Excel", "xlsx");
        if (path == null) return;

        var entity = ExcelImportEntities[ExcelImportEntityIndex];
        AppendLog($"Импорт Excel ({entity.Name}) из {path}...");
        var result = ImportService.ImportExcel(path, entity.Key);
        AppendResult(result);
    }

    private async Task ImportJsonAsync()
    {
        var path = await FileDialogHelper.PickOpenFileAsync("Импорт JSON", "json");
        if (path == null) return;

        AppendLog($"Импорт JSON (полная структура) из {path}...");
        var result = ImportService.ImportJson(path);
        AppendResult(result);
    }

    private async Task ImportXmlAsync()
    {
        var path = await FileDialogHelper.PickOpenFileAsync("Импорт XML", "xml");
        if (path == null) return;

        AppendLog($"Импорт XML (полная структура) из {path}...");
        var result = ImportService.ImportXml(path);
        AppendResult(result);
    }

    private async Task ExportCsvAsync()
    {
        var path = await FileDialogHelper.PickSaveFileAsync("Экспорт CSV", "csv");
        if (path == null) return;

        var entity = CsvExportEntities[CsvExportEntityIndex];
        AppendLog(ExportService.ExportCsv(path, entity.Key));
    }

    private async Task ExportExcelAsync()
    {
        var path = await FileDialogHelper.PickSaveFileAsync("Экспорт Excel", "xlsx");
        if (path == null) return;

        AppendLog(ExportService.ExportExcel(path));
    }

    private async Task ExportPdfAsync()
    {
        var path = await FileDialogHelper.PickSaveFileAsync("Экспорт PDF", "pdf");
        if (path == null) return;

        AppendLog(ExportService.ExportPdf(path));
    }

    private void AppendResult(ImportResult result)
    {
        AppendLog(result.Summary);
        foreach (var error in result.Errors)
            AppendLog("  ⚠ " + error);
        AppendLog("");
    }

    private void AppendLog(string message)
    {
        Log += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }
}
