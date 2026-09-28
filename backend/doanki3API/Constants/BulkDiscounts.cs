namespace doanki3API.Constants;

/// <summary>
/// Bậc chiết khấu bulk/corporate theo spec: "if 10-15 connections are taken, they
/// will be getting a discount of 25% in the advance and as well in the security
/// deposit. If it is between 15-25 connections, then 50% discount and for 25-50
/// then 75% discount and if it is above 50 they will avail 100% discount".
/// Biên chồng lấn (10-15 &amp; 15-25 &amp; 25-50 dùng chung mốc) được chốt:
/// &gt;50 ⇒ 100, ≥25 ⇒ 75, ≥15 ⇒ 50, ≥10 ⇒ 25, còn lại 0.
/// </summary>
public static class BulkDiscounts
{
    public static decimal Compute(int connections)
    {
        if (connections > 50) return 100m;
        if (connections >= 25) return 75m;
        if (connections >= 15) return 50m;
        if (connections >= 10) return 25m;
        return 0m;
    }
}
