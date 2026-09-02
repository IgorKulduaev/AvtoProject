using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSalesApp.Models;

public class Model
{
    public int ModelId { get; set; }
    public string ModelCode { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Upholstery { get; set; } = string.Empty;
    public string MotorPower { get; set; } = string.Empty;
    public int DoorCount { get; set; }
    public string Transmission { get; set; } = string.Empty; // "manual" или "automatic"
}