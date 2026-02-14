using System.Text.Json;

namespace MaxPayroll.SiteEvaluator.Models.Wizard;

/// <summary>
/// Typed state for the Site Evaluator Wizard.
/// Steps: Address ? Property Match ? Zoning ? Hazards ? Geotech ? Infrastructure ? Climate ? Summary
/// </summary>
public class SiteEvaluatorWizardState
{
    /// <summary>
    /// Current step in the wizard (1-8).
    /// </summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Unique session ID for this wizard run.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Step 1: Address entry
    /// </summary>
    public AddressInput Address { get; set; } = new();

    /// <summary>
    /// Step 2: Property matching - existing jobs/evaluations or LINZ matches
    /// </summary>
    public PropertyMatchResult? PropertyMatch { get; set; }

    /// <summary>
    /// The current job being worked on (new Job-based architecture).
    /// </summary>
    public EvaluationJob? Job { get; set; }

    /// <summary>
    /// The location for this job (shared across jobs).
    /// </summary>
    public PropertyLocation? Location { get; set; }

    /// <summary>
    /// [LEGACY] The evaluation being built or retrieved.
    /// Kept for backwards compatibility during transition.
    /// </summary>
    public SiteEvaluation? Evaluation { get; set; }

    /// <summary>
    /// Whether the wizard has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Timestamp when wizard was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Customer information for the job.
    /// </summary>
    public JobCustomerInfo CustomerInfo { get; set; } = new();
}

/// <summary>
/// Customer information captured during wizard.
/// </summary>
public class JobCustomerInfo
{
    public string? CustomerName { get; set; }
    public string? CustomerCompany { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerReference { get; set; }
}

/// <summary>
/// Address input from Step 1.
/// </summary>
public class AddressInput
{
    /// <summary>
    /// Full street address entered by user.
    /// </summary>
    public string FullAddress { get; set; } = "";

    /// <summary>
    /// Title reference (optional, for title-based search).
    /// </summary>
    public string? TitleReference { get; set; }

    /// <summary>
    /// Coordinates (optional, for coordinate-based search).
    /// </summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// Search type selected by user.
    /// </summary>
    public SearchType SearchType { get; set; } = SearchType.Address;

    /// <summary>
    /// Intended use for the property - drives evaluation priorities.
    /// </summary>
    public IntendedPropertyUse IntendedUse { get; set; } = new();
}

/// <summary>
/// Intended use for the property being evaluated.
/// This drives which data is most relevant and what checks to perform.
/// </summary>
public class IntendedPropertyUse
{
    /// <summary>
    /// Primary use category (Residential, Commercial, Industrial, etc.)
    /// </summary>
    public PropertyUseCategory Category { get; set; } = PropertyUseCategory.Residential;

    /// <summary>
    /// Specific use type within the category.
    /// </summary>
    public string? SpecificUse { get; set; }

    /// <summary>
    /// Purpose of the evaluation (Purchase, Development, Due Diligence, etc.)
    /// </summary>
    public EvaluationPurpose Purpose { get; set; } = EvaluationPurpose.Purchase;

    /// <summary>
    /// Is this for a new development or existing use?
    /// </summary>
    public bool IsNewDevelopment { get; set; }

    /// <summary>
    /// Estimated building height for new developments (meters).
    /// Used to check against height limits.
    /// </summary>
    public double? ProposedHeight { get; set; }

    /// <summary>
    /// Estimated site coverage percentage for new developments.
    /// Used to check against coverage limits.
    /// </summary>
    public double? ProposedCoverage { get; set; }

    /// <summary>
    /// Number of units/dwellings for residential developments.
    /// </summary>
    public int? ProposedUnits { get; set; }

    /// <summary>
    /// Gross floor area for commercial/industrial developments (m²).
    /// </summary>
    public double? ProposedGfa { get; set; }

    /// <summary>
    /// Additional notes about intended use.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Primary property use categories.
/// </summary>
public enum PropertyUseCategory
{
    /// <summary>Single dwelling, multi-unit, or residential subdivision</summary>
    Residential,
    
    /// <summary>Retail, office, hospitality</summary>
    Commercial,
    
    /// <summary>Manufacturing, warehousing, logistics</summary>
    Industrial,
    
    /// <summary>Mixed residential and commercial</summary>
    MixedUse,
    
    /// <summary>Farming, horticulture, viticulture</summary>
    Rural,
    
    /// <summary>Schools, hospitals, community facilities</summary>
    Community,
    
    /// <summary>Parks, reserves, conservation</summary>
    OpenSpace,
    
    /// <summary>General inquiry or not specified</summary>
    Other
}

/// <summary>
/// Purpose of the site evaluation.
/// </summary>
public enum EvaluationPurpose
{
    /// <summary>Buying the property</summary>
    Purchase,
    
    /// <summary>Selling the property - understanding constraints</summary>
    Sale,
    
    /// <summary>New development or redevelopment</summary>
    Development,
    
    /// <summary>Subdivision of existing land</summary>
    Subdivision,
    
    /// <summary>General due diligence or research</summary>
    DueDiligence,
    
    /// <summary>Pre-application for resource consent</summary>
    ResourceConsent,
    
    /// <summary>Insurance or risk assessment</summary>
    Insurance,
    
    /// <summary>Valuation support</summary>
    Valuation,
    
    /// <summary>Other purpose</summary>
    Other
}

/// <summary>
/// Type of search being performed.
/// </summary>
public enum SearchType
{
    Address,
    Title,
    Coordinates,
    ExistingEvaluation
}

/// <summary>
/// Property matching results from Step 2.
/// </summary>
public class PropertyMatchResult
{
    /// <summary>
    /// Existing jobs found for this address/location.
    /// </summary>
    public List<ExistingJobMatch> ExistingJobs { get; set; } = [];

    /// <summary>
    /// [LEGACY] Existing evaluations found for this address/location.
    /// </summary>
    public List<ExistingEvaluationMatch> ExistingEvaluations { get; set; } = [];

    /// <summary>
    /// LINZ property matches for disambiguation.
    /// </summary>
    public List<LinzPropertyMatch> LinzMatches { get; set; } = [];

    /// <summary>
    /// Existing location if found (can reuse for new job).
    /// </summary>
    public PropertyLocation? ExistingLocation { get; set; }

    /// <summary>
    /// Selected property (after user selection or auto-select if only one).
    /// </summary>
    public LinzPropertyMatch? SelectedProperty { get; set; }

    /// <summary>
    /// Whether user chose to create new job vs using existing.
    /// </summary>
    public bool CreateNew { get; set; } = true;

    /// <summary>
    /// Selected existing job ID (if continuing existing job).
    /// </summary>
    public string? SelectedJobId { get; set; }

    /// <summary>
    /// [LEGACY] Selected existing evaluation ID (if using existing).
    /// </summary>
    public string? SelectedExistingId { get; set; }
}

/// <summary>
/// Match with an existing job in the system.
/// </summary>
public class ExistingJobMatch
{
    public string Id { get; set; } = "";
    public string JobReference { get; set; } = "";
    public string Address { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? CustomerCompany { get; set; }
    public JobPurpose Purpose { get; set; }
    public JobStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public int CompletenessPercent { get; set; }
    public int ReportCount { get; set; }
}

/// <summary>
/// Match with an existing evaluation in the system.
/// </summary>
public class ExistingEvaluationMatch
{
    public string Id { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public int CompletenessPercent { get; set; }
    public EvaluationStatus Status { get; set; }
}

/// <summary>
/// Match with LINZ property data.
/// </summary>
public class LinzPropertyMatch
{
    public string FullAddress { get; set; } = "";
    public string? TitleReference { get; set; }
    public string? LegalDescription { get; set; }
    public string? TerritorialAuthority { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    
    /// <summary>
    /// Confidence score (0-100) for address match quality.
    /// </summary>
    public int MatchConfidence { get; set; }
}

/// <summary>
/// Extension methods for storing/retrieving wizard state from TempData.
/// </summary>
public static class SiteEvaluatorWizardExtensions
{
    private const string WizardStateKey = "SiteEvaluatorWizardState";

    /// <summary>
    /// Gets the wizard state from TempData, or creates a new one.
    /// </summary>
    public static SiteEvaluatorWizardState GetSiteEvaluatorWizardState(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData)
    {
        if (tempData.TryGetValue(WizardStateKey, out var value) && value is string json)
        {
            tempData.Keep(WizardStateKey); // Keep for next request
            return JsonSerializer.Deserialize<SiteEvaluatorWizardState>(json) ?? new SiteEvaluatorWizardState();
        }
        return new SiteEvaluatorWizardState();
    }

    /// <summary>
    /// Saves the wizard state to TempData.
    /// </summary>
    public static void SetSiteEvaluatorWizardState(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, SiteEvaluatorWizardState state)
    {
        state.LastActivityAt = DateTime.UtcNow;
        tempData[WizardStateKey] = JsonSerializer.Serialize(state);
    }

    /// <summary>
    /// Clears the wizard state from TempData.
    /// </summary>
    public static void ClearSiteEvaluatorWizardState(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData)
    {
        tempData.Remove(WizardStateKey);
    }

    /// <summary>
    /// Peeks at the wizard state without consuming it.
    /// </summary>
    public static SiteEvaluatorWizardState PeekSiteEvaluatorWizardState(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData)
    {
        if (tempData.Peek(WizardStateKey) is string json)
        {
            return JsonSerializer.Deserialize<SiteEvaluatorWizardState>(json) ?? new SiteEvaluatorWizardState();
        }
        return new SiteEvaluatorWizardState();
    }
}
