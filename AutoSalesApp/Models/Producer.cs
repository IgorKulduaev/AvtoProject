using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSalesApp.Models;

public class Producer
{
    public int ProducerId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
}