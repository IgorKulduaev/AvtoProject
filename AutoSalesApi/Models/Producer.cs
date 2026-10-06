using System.ComponentModel.DataAnnotations;

namespace AutoSalesApi.Models;

public class Producer
{
    public int ProducerId { get; set; }

    [Required]
    public string CompanyCode { get; set; } = string.Empty;

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Website { get; set; }

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
