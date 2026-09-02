using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoSalesApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object _currentProducerView = new ProducerViewModel();

    [ObservableProperty]
    private object _currentModelView = new ModelViewModel();

    [ObservableProperty]
    private object _currentClientView = new ClientViewModel();

    [ObservableProperty]
    private object _currentPriceView = new PriceViewModel();

    [ObservableProperty]
    private object _currentOrderView = new SaleViewModel();

    [ObservableProperty]
    private object _currentReportView = new ReportViewModel();

    [ObservableProperty]
    private object _currentChartView = new ChartViewModel();

    [ObservableProperty]
    private object _currentImportExportView = new ImportExportViewModel();
}
