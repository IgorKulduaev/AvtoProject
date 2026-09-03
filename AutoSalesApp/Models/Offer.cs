using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSalesApp.Models;

public class Offer
{
    public int OfferId { get; set; }
    public int ProducerId { get; set; }
    public int ModelId { get; set; }

    public Producer? Producer { get; set; }
    public Model? Model { get; set; }
}
