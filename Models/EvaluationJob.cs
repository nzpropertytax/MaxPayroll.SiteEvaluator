using MaxPayroll.SiteEvaluator.Models.Wizard;
using MaxPayroll.SiteEvaluator.Services;

namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// A Job represents a single engagement/request for site evaluation.
/// Multiple jobs can exist for the same location, for different customers or purposes.
/// </summary>
public class EvaluationJob
{
    public string Id { get; set; } = GenerateJobId();
    
    // === Job Identification ===
    
    /// <summary>
    /// Human-readable job reference (e.g., "JOB-2025-00123")
    /// </summary>
    public string JobReference { get; set; } = string.Empty;
    
    /// <summary>
    /// Job title/name for easy identification
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    // === Location Reference ===
    
    /// <summary>
    /// Reference to the location record (shared across jobs for same property)
    /// </summary>
    public string LocationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Denormalized address for display (avoids joins)
    /// </summary>
    public string Address { get; set; } = string.Empty;
    
    // === Customer Information ===
    
    /// <summary>
    /// Customer/client name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer reference (their PO number, file reference, etc.)
    /// </summary>
    public string? CustomerReference { get; set; }
    
    /// <summary>
    /// Customer email for report delivery
    /// </summary>
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Customer company/organization
    /// </summary>
    public string? CustomerCompany { get; set; }
    
    // === Job Context ===
    
    /// <summary>
    /// Purpose of this evaluation (Purchase, Development, etc.)
    /// </summary>
    public JobPurpose Purpose { get; set; } = JobPurpose.GeneralEnquiry;
    
    /// <summary>
    /// Detailed description of what's being evaluated
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Intended property use category
    /// </summary>
    public PropertyUseCategory IntendedUse { get; set; } = PropertyUseCategory.Residential;
    
    /// <summary>
    /// Specific intended use details
    /// </summary>
    public string? IntendedUseDetails { get; set; }
    
    /// <summary>
    /// Is this for a new development or existing property?
    /// </summary>
    public bool IsNewDevelopment { get; set; }
    
    // === Proposed Development Details (if applicable) ===
    
    /// <summary>
    /// Proposed building height (meters)
    /// </summary>
    public double? ProposedHeight { get; set; }
    
    /// <summary>
    /// Proposed site coverage (%)
    /// </summary>
    public double? ProposedCoverage { get; set; }
    
    /// <summary>
    /// Proposed number of units/dwellings
    /// </summary>
    public int? ProposedUnits { get; set; }
    
    /// <summary>
    /// Proposed gross floor area (m²)
    /// </summary>
    public double? ProposedGfa { get; set; }
    
    // === Status & Tracking ===
    
    public JobStatus Status { get; set; } = JobStatus.Created;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    
    /// <summary>
    /// User who created the job
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// User who created the job (display name)
    /// </summary>
    public string CreatedByUserName { get; set; } = string.Empty;
    
    // === Data Collection Status ===
    
    /// <summary>
    /// Which data sections have been collected for this job
    /// </summary>
    public JobDataStatus DataStatus { get; set; } = new();
    
    /// <summary>
    /// Overall data completeness percentage
    /// </summary>
    public int CompletenessPercent { get; set; }
    
    /// <summary>
    /// Warnings/issues found during evaluation
    /// </summary>
    public List<string> Warnings { get; set; } = [];
    
    // === Reports Generated ===
    
    /// <summary>
    /// Reports generated for this job
    /// </summary>
    public List<JobReport> Reports { get; set; } = [];
    
    // === Billing (optional) ===
    
    /// <summary>
    /// Is this job billable?
    /// </summary>
    public bool IsBillable { get; set; } = true;
    
    /// <summary>
    /// Billing status
    /// </summary>
    public BillingStatus BillingStatus { get; set; } = BillingStatus.NotBilled;
    
    /// <summary>
    /// Invoice reference (if billed)
    /// </summary>
    public string? InvoiceReference { get; set; }
    
    // === Notes ===
    
    /// <summary>
    /// Internal notes about the job
    /// </summary>
    public string? InternalNotes { get; set; }
    
    // === Helpers ===
    
    private static string GenerateJobId() => $"job_{Guid.NewGuid():N}";
    
    public static string GenerateJobReference(int sequenceNumber)
    {
        return $"JOB-{DateTime.UtcNow:yyyy}-{sequenceNumber:D5}";
    }
}

/// <summary>
/// Status of data collection for each section
/// </summary>
public class JobDataStatus
{
    public DataSectionStatus Location { get; set; } = DataSectionStatus.Pending;
    public DataSectionStatus Zoning { get; set; } = DataSectionStatus.Pending;
    public DataSectionStatus Hazards { get; set; } = DataSectionStatus.Pending;
    public DataSectionStatus Geotech { get; set; } = DataSectionStatus.Pending;
    public DataSectionStatus Infrastructure { get; set; } = DataSectionStatus.Pending;
    public DataSectionStatus Climate { get; set; } = DataSectionStatus.Pending;
    public DataSectionStatus Land { get; set; } = DataSectionStatus.Pending;
    
    public DateTime? LocationUpdated { get; set; }
    public DateTime? ZoningUpdated { get; set; }
    public DateTime? HazardsUpdated { get; set; }
    public DateTime? GeotechUpdated { get; set; }
    public DateTime? InfrastructureUpdated { get; set; }
    public DateTime? ClimateUpdated { get; set; }
    public DateTime? LandUpdated { get; set; }
}

public enum DataSectionStatus
{
    Pending,
    InProgress,
    Complete,
    Partial,
    Failed,
    NotAvailable
}

/// <summary>
/// A report generated for a job
/// </summary>
public class JobReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string JobId { get; set; } = string.Empty;
    
    public ReportType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    public string GeneratedByUserId { get; set; } = string.Empty;
    public string GeneratedByUserName { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Storage path or blob URL
    /// </summary>
    public string? StoragePath { get; set; }
    
    /// <summary>
    /// Options used to generate this report
    /// </summary>
    public ReportOptions Options { get; set; } = new();
    
    /// <summary>
    /// Download count
    /// </summary>
    public int DownloadCount { get; set; }
    
    /// <summary>
    /// Last downloaded date
    /// </summary>
    public DateTime? LastDownloaded { get; set; }
}

public enum ReportType
{
    FullReport,
    SummaryReport,
    GeotechBrief,
    DueDiligencePack,
    Custom
}

public enum JobStatus
{
    Created,
    InProgress,
    DataCollection,
    Review,
    Complete,
    Cancelled,
    OnHold
}

public enum JobPurpose
{
    GeneralEnquiry,
    Purchase,
    Sale,
    Development,
    Subdivision,
    ResourceConsent,
    BuildingConsent,
    DueDiligence,
    Insurance,
    Valuation,
    SiteInvestigation,
    Other
}

public enum BillingStatus
{
    NotBilled,
    Pending,
    Invoiced,
    Paid,
    Waived,
    Disputed
}

// Note: PropertyUseCategory is defined in MaxPayroll.SiteEvaluator.Models.Wizard namespace
