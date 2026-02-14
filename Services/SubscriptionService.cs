using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Subscription and billing service using self-contained repository.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly ISiteEvaluatorRepository _repository;
    private readonly ILogger<SubscriptionService> _logger;
    
    private const decimal PayPerSearchPrice = 25.00m;

    public SubscriptionService(ISiteEvaluatorRepository repository, ILogger<SubscriptionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> CanSearchAsync(string userId, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId);
        
        // Reset monthly usage if needed
        if (DateTime.UtcNow >= subscription.UsageResetDate)
        {
            subscription.SearchesThisMonth = 0;
            subscription.ReportsThisMonth = 0;
            subscription.UsageResetDate = DateTime.UtcNow.AddMonths(1);
            await _repository.UpdateAsync(subscription);
        }

        var searchLimit = SubscriptionTierConfig.GetSearchesPerMonth(subscription.Tier);

        // Free tier can always use pay-per-search
        if (subscription.Tier == SubscriptionTier.Free)
            return true;

        // Check if within monthly limit
        return subscription.SearchesThisMonth < searchLimit;
    }

    public async Task RecordSearchUsageAsync(string userId, string evaluationId, SearchType searchType, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId);
        
        // Increment usage counter
        subscription.SearchesThisMonth++;
        subscription.LastSearchDate = DateTime.UtcNow;
        await _repository.UpdateAsync(subscription);

        // Create usage record
        var usageRecord = new SearchUsageRecord
        {
            UserId = userId,
            EvaluationId = evaluationId,
            SearchType = searchType,
            WasBilled = subscription.Tier == SubscriptionTier.Free,
            AmountCharged = subscription.Tier == SubscriptionTier.Free ? PayPerSearchPrice : null
        };

        await _repository.InsertAsync(usageRecord);

        _logger.LogInformation("Recorded search usage for user {UserId}, evaluation {EvaluationId}", userId, evaluationId);
    }

    public async Task<SiteEvaluatorSubscription?> GetSubscriptionAsync(string userId, CancellationToken ct = default)
    {
        var all = await _repository.FindAsync<SiteEvaluatorSubscription>(s => s.UserId == userId);
        return all.FirstOrDefault();
    }

    public async Task<bool> ProcessPayPerSearchAsync(string userId, CancellationToken ct = default)
    {
        var subscription = await GetSubscriptionAsync(userId, ct);
        
        if (subscription == null)
        {
            _logger.LogError("Cannot process payment: Subscription not found for user: {UserId}", userId);
            return false;
        }

        // In a real implementation, this would integrate with Stripe
        _logger.LogInformation("Processing pay-per-search payment of ${Price} for user {UserId}", PayPerSearchPrice, userId);
        
        // TODO: Implement Stripe payment processing

        return true;
    }

    /// <summary>
    /// Get usage summary for a user.
    /// </summary>
    public async Task<UsageSummary> GetUsageSummaryAsync(string userId, CancellationToken ct = default)
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId);
        var searchLimit = SubscriptionTierConfig.GetSearchesPerMonth(subscription.Tier);

        return new UsageSummary
        {
            Tier = subscription.Tier,
            SearchesUsed = subscription.SearchesThisMonth,
            SearchesLimit = searchLimit,
            ReportsUsed = subscription.ReportsThisMonth,
            ResetDate = subscription.UsageResetDate
        };
    }

    private async Task<SiteEvaluatorSubscription> GetOrCreateSubscriptionAsync(string userId)
    {
        var subscription = await GetSubscriptionAsync(userId);
        
        if (subscription == null)
        {
            subscription = new SiteEvaluatorSubscription
            {
                UserId = userId,
                Tier = SubscriptionTier.Free
            };
            await _repository.InsertAsync(subscription);
        }

        return subscription;
    }
}

public class UsageSummary
{
    public SubscriptionTier Tier { get; set; }
    public int SearchesUsed { get; set; }
    public int SearchesLimit { get; set; }
    public int ReportsUsed { get; set; }
    public DateTime ResetDate { get; set; }
    
    public int SearchesRemaining => Math.Max(0, SearchesLimit - SearchesUsed);
    public bool IsUnlimited => SearchesLimit == int.MaxValue;
}
