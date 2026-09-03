using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSalesApp.Models;

public class PriceList
{
    public int PriceId { get; set; }
    public int ModelId { get; set; }
    public int YearOfManufacture { get; set; }
    public decimal Price { get; set; }
    public decimal PrepCost { get; set; }
    public decimal TransportCost { get; set; }

    public Model? Model { get; set; }
}
