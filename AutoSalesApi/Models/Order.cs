using System.ComponentModel.DataAnnotations;

namespace AutoSalesApi.Models;

public class Order
{
    public int OrderId { get; set; }

    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }

    public int ModelId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalCost { get; set; }

    public Client? Client { get; set; }

    public Model? Model { get; set; }
}
