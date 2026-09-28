namespace doanki3API.Constants;

public static class ConnectionStatuses
{
    public const string Active = "Active";
    public const string TemporarilyInactive = "Temporarily Inactive";
    public const string PermanentlyInactive = "Permanently Inactive";

    public static readonly string[] All =
    [
        Active,
        TemporarilyInactive,
        PermanentlyInactive
    ];
}
