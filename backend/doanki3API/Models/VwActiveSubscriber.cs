using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class VwActiveSubscriber
{
    public string AccountId { get; set; } = null!;

    public string OrderId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string CustomerType { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string InstallationAddress { get; set; } = null!;

    public string PlanName { get; set; } = null!;

    public string ConnectionType { get; set; } = null!;

    public decimal MonthlyRental { get; set; }

    public decimal SecurityDeposit { get; set; }

    public string ConnectionStatus { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? PortNumber { get; set; }

    public string? DeviceSerial { get; set; }

    public string? DeviceModel { get; set; }

    public string RetailStoreName { get; set; } = null!;

    public DateTime InstalledDate { get; set; }

    public DateTime LastUpdated { get; set; }
}
