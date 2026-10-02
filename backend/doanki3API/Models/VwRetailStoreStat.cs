using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class VwRetailStoreStat
{
    public string StoreId { get; set; } = null!;

    public string StoreName { get; set; } = null!;

    public string City { get; set; } = null!;

    public string CityCode { get; set; } = null!;

    public string? ManagerName { get; set; }

    public string StoreStatus { get; set; } = null!;

    public int? ActualEmployeeCount { get; set; }

    public int? TotalOrdersCount { get; set; }

    public int? TotalSubscribersCount { get; set; }
}
