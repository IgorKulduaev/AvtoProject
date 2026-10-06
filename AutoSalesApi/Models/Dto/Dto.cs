namespace AutoSalesApi.Models.Dto;

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

public class ModelWithPrice
{
    public int ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public int YearOfManufacture { get; set; }
    public decimal TotalCost { get; set; }
}

public class OrderItem
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ClientFio { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalCost { get; set; }
}

public class ReportResult
{
    public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = Array.Empty<IReadOnlyList<string>>();
}
