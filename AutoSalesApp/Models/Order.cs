using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSalesApp.Models;

public class Order
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public int ModelId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalCost { get; set; }

    public Client? Client { get; set; }
    public Model? Model { get; set; }
}
