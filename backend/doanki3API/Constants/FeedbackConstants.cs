namespace doanki3API.Constants;

/// <summary>Đúng các giá trị mà CK_Feedback_Category cho phép.</summary>
public static class FeedbackCategories
{
    public const string Other = "Other";

    public static readonly string[] All =
    [
        "Service Quality",
        "Installation",
        "Billing",
        "Support",
        Other
    ];
}

/// <summary>Đúng các giá trị mà CK_Feedback_Status cho phép ("Pending" KHÔNG hợp lệ).</summary>
public static class FeedbackStatuses
{
    public const string New = "New";
    public const string UnderReview = "Under Review";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";

    public static readonly string[] All = [New, UnderReview, Resolved, Closed];
}
