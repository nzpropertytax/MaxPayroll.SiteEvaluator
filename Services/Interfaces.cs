using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Models.Wizard;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Manages evaluation jobs - the primary entry point for new evaluations.
/// </summary>
public interface IJobService
{
    // === Job CRUD ===
    
    /// <summary>
    /// Create a new evaluation job.
    /// </summary>
    Task<EvaluationJob> CreateJobAsync(CreateJobRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Get a job by ID.
    /// </summary>
    Task<EvaluationJob?> GetJobAsync(string jobId, CancellationToken ct = default);
    
    /// <summary>
    /// Get a job by reference number (e.g., "JOB-2025-00123").
    /// </summary>
    Task<EvaluationJob?> GetJobByReferenceAsync(string reference, CancellationToken ct = default);
    
    /// <summary>
    /// Update job details.
    /// </summary>
    Task<EvaluationJob> UpdateJobAsync(string jobId, UpdateJobRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Delete/cancel a job.
    /// </summary>
    Task<bool> DeleteJobAsync(string jobId, CancellationToken ct = default);
    
    // === Job Queries ===
    
    /// <summary>
    /// List jobs for a user.
    /// </summary>
    Task<IEnumerable<EvaluationJob>> GetUserJobsAsync(string userId, JobListFilter? filter = null, CancellationToken ct = default);
    
    /// <summary>
    /// List jobs for a specific location.
    /// </summary>
    Task<IEnumerable<EvaluationJob>> GetJobsForLocationAsync(string locationId, CancellationToken ct = default);
    
    /// <summary>
    /// Search jobs by customer name, reference, or address.
    /// </summary>
    Task<IEnumerable<EvaluationJob>> SearchJobsAsync(string query, CancellationToken ct = default);
    
    // === Data Collection ===
    
    /// <summary>
    /// Start data collection for a job (runs all API calls).
    /// </summary>
    Task<EvaluationJob> StartDataCollectionAsync(string jobId, CancellationToken ct = default);
    
    /// <summary>
    /// Refresh specific data sections for a job.
    /// </summary>
    Task<EvaluationJob> RefreshDataSectionsAsync(string jobId, IEnumerable<string> sections, CancellationToken ct = default);
    
    // === Reports ===
    
    /// <summary>
    /// Generate a report for a job.
    /// </summary>
    Task<JobReport> GenerateReportAsync(string jobId, ReportType type, ReportOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Get report file content.
    /// </summary>
    Task<byte[]?> GetReportContentAsync(string jobId, string reportId, CancellationToken ct = default);
    
    // === Status ===
    
    /// <summary>
    /// Update job status.
    /// </summary>
    Task<EvaluationJob> UpdateJobStatusAsync(string jobId, JobStatus status, CancellationToken ct = default);
    
    /// <summary>
    /// Get next job reference number.
    /// </summary>
    Task<string> GetNextJobReferenceAsync(CancellationToken ct = default);
}

/// <summary>
/// Request to create a new job.
/// </summary>
public class CreateJobRequest
{
    // Location (one of these required)
    public string? Address { get; set; }
    public string? TitleReference { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ExistingLocationId { get; set; }
    
    // Job details
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerReference { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerCompany { get; set; }
    public JobPurpose Purpose { get; set; } = JobPurpose.GeneralEnquiry;
    public string? Description { get; set; }
    
    // Intended use
    public PropertyUseCategory IntendedUse { get; set; } = PropertyUseCategory.Residential;
    public string? IntendedUseDetails { get; set; }
    public bool IsNewDevelopment { get; set; }
    
    // Development details (if applicable)
    public double? ProposedHeight { get; set; }
    public double? ProposedCoverage { get; set; }
    public int? ProposedUnits { get; set; }
    public double? ProposedGfa { get; set; }
    
    // Options
    public bool AutoStartDataCollection { get; set; } = true;
    public bool IsBillable { get; set; } = true;
    public string? InternalNotes { get; set; }
}

/// <summary>
/// Request to update a job.
/// </summary>
public class UpdateJobRequest
{
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerReference { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerCompany { get; set; }
    public JobPurpose? Purpose { get; set; }
    public string? Description { get; set; }
    public PropertyUseCategory? IntendedUse { get; set; }
    public string? IntendedUseDetails { get; set; }
    public bool? IsNewDevelopment { get; set; }
    public double? ProposedHeight { get; set; }
    public double? ProposedCoverage { get; set; }
    public int? ProposedUnits { get; set; }
    public double? ProposedGfa { get; set; }
    public bool? IsBillable { get; set; }
    public string? InternalNotes { get; set; }
}

/// <summary>
/// Filter for job list queries.
/// </summary>
public class JobListFilter
{
    public JobStatus? Status { get; set; }
    public JobPurpose? Purpose { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? CustomerName { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
    public string SortBy { get; set; } = "CreatedDate";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Manages property locations (shared across jobs).
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Get or create a location by address.
    /// </summary>
    Task<PropertyLocation> GetOrCreateByAddressAsync(string address, CancellationToken ct = default);
    
    /// <summary>
    /// Get or create a location by title reference.
    /// </summary>
    Task<PropertyLocation> GetOrCreateByTitleAsync(string titleReference, CancellationToken ct = default);
    
    /// <summary>
    /// Get or create a location by coordinates.
    /// </summary>
    Task<PropertyLocation> GetOrCreateByCoordinatesAsync(double lat, double lon, CancellationToken ct = default);
    
    /// <summary>
    /// Get a location by ID.
    /// </summary>
    Task<PropertyLocation?> GetLocationAsync(string locationId, CancellationToken ct = default);
    
    /// <summary>
    /// Find existing locations near coordinates.
    /// </summary>
    Task<IEnumerable<PropertyLocation>> FindNearbyLocationsAsync(double lat, double lon, double radiusMeters = 50, CancellationToken ct = default);
    
    /// <summary>
    /// Refresh cached data for a location.
    /// </summary>
    Task<PropertyLocation> RefreshLocationDataAsync(string locationId, IEnumerable<string>? sections = null, CancellationToken ct = default);
    
    /// <summary>
    /// Get location summaries (for list views).
    /// </summary>
    Task<IEnumerable<LocationSummary>> GetLocationSummariesAsync(int skip = 0, int take = 50, CancellationToken ct = default);
}

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
    Task RecordSearchUsageAsync(string userId, string evaluationId, Models.SearchType searchType, CancellationToken ct = default);
    
    /// <summary>
    /// Get user's Site Evaluator subscription.
    /// </summary>
    Task<SiteEvaluatorSubscription?> GetSubscriptionAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Process a pay-per-search payment.
    /// </summary>
    Task<bool> ProcessPayPerSearchAsync(string userId, CancellationToken ct = default);
}
