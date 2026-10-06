using System.ComponentModel.DataAnnotations;

namespace AutoSalesApi.Models;

public class PriceList
{
    public int PriceId { get; set; }

    public int ModelId { get; set; }

    [Range(1950, 2100)]
    public int YearOfManufacture { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrepCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TransportCost { get; set; }

    public Model? Model { get; set; }

    public decimal TotalCost => Price + PrepCost + TransportCost;
}
