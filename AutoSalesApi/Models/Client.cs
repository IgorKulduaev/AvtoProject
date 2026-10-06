using System.ComponentModel.DataAnnotations;

namespace AutoSalesApi.Models;

public class Client
{
    public int ClientId { get; set; }

    [Required]
    public string FIO { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
