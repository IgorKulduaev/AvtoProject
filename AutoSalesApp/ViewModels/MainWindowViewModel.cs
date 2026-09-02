using System;
using System.Collections.Generic;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoSalesApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object _currentProducerView = new ProducerViewModel();

    [ObservableProperty]
    private object _currentPriceView = new object();

    [ObservableProperty]
    private object _currentOrderView = new object();

    [ObservableProperty]
    private object _currentReportView = new object();

    [ObservableProperty]
    private object _currentChartView = new object();

    [ObservableProperty]
    private object _currentImportExportView = new object();
    [ObservableProperty]
    private object _currentModelView = new ModelViewModel();
    [ObservableProperty]
    private object _currentClientView = new ClientViewModel();
}