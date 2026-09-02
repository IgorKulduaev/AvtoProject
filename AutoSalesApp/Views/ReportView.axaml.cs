using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using AutoSalesApp.ViewModels;

namespace AutoSalesApp.Views;

public partial class ReportView : UserControl
{
    private ReportViewModel? _vm;

    public ReportView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as ReportViewModel;

        if (_vm != null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            RebuildColumns();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReportViewModel.Headers))
            RebuildColumns();
    }

    private void RebuildColumns()
    {
        if (_vm == null) return;

        ReportGrid.Columns.Clear();
        for (int i = 0; i < _vm.Headers.Count; i++)
        {
            ReportGrid.Columns.Add(new DataGridTextColumn
            {
                Header = _vm.Headers[i],
                Binding = new Binding($"Cells[{i}]")
            });
        }
    }
}
