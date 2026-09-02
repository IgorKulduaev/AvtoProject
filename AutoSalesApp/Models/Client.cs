using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSalesApp.Models;

public class Client
{
    public int ClientId { get; set; }
    public string FIO { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}