using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class SystemSetting
{
    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime LastUpdated { get; set; }
}
