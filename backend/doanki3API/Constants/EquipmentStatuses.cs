namespace doanki3API.Constants;

public static class EquipmentStatuses
{
    public const string InStock = "In Stock";
    public const string InService = "In Service";
    public const string Maintenance = "Maintenance";
    public const string Faulty = "Faulty";

    public static readonly string[] All =
    [
        InStock,
        InService,
        Maintenance,
        Faulty
    ];
}
