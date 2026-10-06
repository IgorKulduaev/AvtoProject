using System.ComponentModel.DataAnnotations;

namespace AutoSalesApi.Models;

public class Model
{
    public int ModelId { get; set; }

    [Required]
    public string ModelCode { get; set; } = string.Empty;

    [Required]
    public string ModelName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Upholstery { get; set; } = string.Empty;

    public string MotorPower { get; set; } = string.Empty;

    public int DoorCount { get; set; }

    public string Transmission { get; set; } = string.Empty;

    public PriceList? PriceList { get; set; }

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
