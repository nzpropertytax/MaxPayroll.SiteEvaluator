using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Main service for site evaluation searches.
/// </summary>
public interface ISiteSearchService
{
    /// <summary>
    /// Search for a site by address.
    /// </summary>
    Task<SiteEvaluation> SearchByAddressAsync(string address, CancellationToken ct = default);
    
    /// <summary>
    /// Search for a site by title reference.
    /// </summary>
    Task<SiteEvaluation> SearchByTitleAsync(string titleReference, CancellationToken ct = default);
    
    /// <summary>
    /// Search for a site by coordinates (map click).
    /// </summary>
    Task<SiteEvaluation> SearchByCoordinatesAsync(double latitude, double longitude, CancellationToken ct = default);
    
    /// <summary>
    /// Refresh specific data sections for an existing evaluation.
    /// </summary>
    Task<SiteEvaluation> RefreshDataAsync(string evaluationId, IEnumerable<string> sections, CancellationToken ct = default);
    
    /// <summary>
    /// Get a previously saved evaluation.
    /// </summary>
    Task<SiteEvaluation?> GetEvaluationAsync(string evaluationId, CancellationToken ct = default);
    
    /// <summary>
    /// List evaluations for a user.
    /// </summary>
    Task<IEnumerable<SiteEvaluation>> GetUserEvaluationsAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Delete an evaluation.
    /// </summary>
    Task<bool> DeleteEvaluationAsync(string evaluationId, CancellationToken ct = default);
}

/// <summary>
/// Generates PDF and other format reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generate a full site evaluation report.
    /// </summary>
    Task<byte[]> GenerateFullReportAsync(SiteEvaluation evaluation, ReportOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Generate a summary report.
    /// </summary>
    Task<byte[]> GenerateSummaryReportAsync(SiteEvaluation evaluation, CancellationToken ct = default);
    
    /// <summary>
    /// Generate a geotech brief.
    /// </summary>
    Task<byte[]> GenerateGeotechBriefAsync(SiteEvaluation evaluation, CancellationToken ct = default);
    
    /// <summary>
    /// Generate a due diligence pack.
    /// </summary>
    Task<byte[]> GenerateDueDiligencePackAsync(SiteEvaluation evaluation, CancellationToken ct = default);
}

public class ReportOptions
{
    public string? LogoUrl { get; set; }
    public string? CompanyName { get; set; }
    public string? PreparedBy { get; set; }
    public string? PreparedFor { get; set; }
    public List<string> IncludeSections { get; set; } = [];
    public bool IncludeAppendices { get; set; } = true;
    public bool IncludeMaps { get; set; } = true;
}

/// <summary>
/// Manages user subscriptions and billing for Site Evaluator feature.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Check if user can perform a search.
    /// </summary>
    Task<bool> CanSearchAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Record a search usage.
    /// </summary>
    Task RecordSearchUsageAsync(string userId, string evaluationId, SearchType searchType, CancellationToken ct = default);
    
    /// <summary>
    /// Get user's Site Evaluator subscription.
    /// </summary>
    Task<SiteEvaluatorSubscription?> GetSubscriptionAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Process a pay-per-search payment.
    /// </summary>
    Task<bool> ProcessPayPerSearchAsync(string userId, CancellationToken ct = default);
}
