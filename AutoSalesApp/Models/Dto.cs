using System;

namespace AutoSalesApp.Models;

// Строка прейскуранта с названием модели (JOIN PriceList + Model)
public class PriceListItem
{
    public int PriceId { get; set; }
    public int ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int YearOfManufacture { get; set; }
    public decimal Price { get; set; }
    public decimal PrepCost { get; set; }
    public decimal TransportCost { get; set; }
    public decimal TotalCost { get; set; }
}

// Модель с данными прейскуранта (для формы продажи)
public class ModelWithPrice
{
    public int ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public int YearOfManufacture { get; set; }
    public decimal TotalCost { get; set; }
}

// Строка продажи с ФИО клиента и названием модели
public class OrderItem
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ClientFio { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalCost { get; set; }
}

// Универсальная строка отчёта для DataGrid (заголовки задаются отдельно)
public class ReportRow
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}
