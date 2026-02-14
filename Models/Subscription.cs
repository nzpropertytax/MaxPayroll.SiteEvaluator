namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Subscription tracking for site evaluator feature.
/// Stored in the site's LiteDB database.
/// </summary>
public class SiteEvaluatorSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    
    // Subscription
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    
    // Usage tracking
    public int SearchesThisMonth { get; set; }
    public int ReportsThisMonth { get; set; }
    public DateTime UsageResetDate { get; set; } = DateTime.UtcNow.AddMonths(1);
    
    // Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastSearchDate { get; set; }
}

public enum SubscriptionTier
{
    Free,        // Pay-per-search only
    Starter,     // $99/month - 10 searches
    Professional,// $299/month - 50 searches
    Enterprise   // $599/month - Unlimited
}

/// <summary>
/// Subscription tier configuration.
/// </summary>
public static class SubscriptionTierConfig
{
    public static int GetSearchesPerMonth(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 0,
        SubscriptionTier.Starter => 10,
        SubscriptionTier.Professional => 50,
        SubscriptionTier.Enterprise => int.MaxValue,
        _ => 0
    };

    public static decimal GetMonthlyPrice(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 0,
        SubscriptionTier.Starter => 99,
        SubscriptionTier.Professional => 299,
        SubscriptionTier.Enterprise => 599,
        _ => 0
    };

    public static bool HasFullReports(SubscriptionTier tier) => 
        tier >= SubscriptionTier.Professional;

    public static bool HasApiAccess(SubscriptionTier tier) => 
        tier >= SubscriptionTier.Professional;
}

/// <summary>
/// Search usage record for billing/audit.
/// </summary>
public class SearchUsageRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string EvaluationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public SearchType SearchType { get; set; }
    public bool WasBilled { get; set; }
    public decimal? AmountCharged { get; set; }
}

public enum SearchType
{
    AddressSearch,
    TitleSearch,
    CoordinateSearch,
    DataRefresh
}
