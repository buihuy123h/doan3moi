using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Vendor
{
    public string VendorId { get; set; } = null!;

    public string VendorCode { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string Category { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public byte Rating { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
