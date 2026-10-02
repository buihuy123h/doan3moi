using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class RetailStore
{
    public string StoreId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string City { get; set; } = null!;

    public string CityCode { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? ManagerName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? OperatingHours { get; set; }

    public string Status { get; set; } = null!;

    public int ActiveEmployeesCount { get; set; }

    public int TotalSubscribersServed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
