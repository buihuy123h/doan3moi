using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Employee
{
    public string EmployeeId { get; set; } = null!;

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string? StoreId { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly DateOfJoining { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Address { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }



    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> OrderEmployees { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderFeasibilityCheckedByNavigations { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual RetailStore? Store { get; set; }
}
