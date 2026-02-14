using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Models.Wizard;
using MaxPayroll.SiteEvaluator.Services;
using MaxPayroll.SiteEvaluator.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaxPayroll.SiteEvaluator.Pages.SiteEvaluator;

/// <summary>
/// Site Evaluation Wizard - guides users through property due diligence.
/// 
/// Now uses Job-based architecture where:
/// - Each evaluation request = one Job
/// - Locations are shared across jobs
/// - Multiple jobs can exist for the same property
/// 
/// Steps:
/// 1. Address Entry - Enter address, title, or coordinates
/// 2. Property Match - Match to existing jobs/locations or select from LINZ results
/// 3. Zoning - Review zoning and planning data
/// 4. Hazards - Review natural hazards (flood, liquefaction, seismic)
/// 5. Geotech - Review nearby geotechnical data
/// 6. Infrastructure - Review 3 Waters, power, communications
/// 7. Climate - Review wind zones, rainfall, climate data
/// 8. Summary - Complete summary and report generation
/// </summary>
public class EvaluationWizardModel : PageModel
{
    private readonly IJobService _jobService;
    private readonly ILocationService _locationService;
    private readonly ISiteSearchService _searchService;
    private readonly ILinzDataService _linzService;
    private readonly ISiteEvaluatorRepository _repository;
    private readonly ILogger<EvaluationWizardModel> _logger;

    public EvaluationWizardModel(
        IJobService jobService,
        ILocationService locationService,
        ISiteSearchService searchService,
        ILinzDataService linzService,
        ISiteEvaluatorRepository repository,
        ILogger<EvaluationWizardModel> logger)
    {
        _jobService = jobService;
        _locationService = locationService;
        _searchService = searchService;
        _linzService = linzService;
        _repository = repository;
        _logger = logger;
    }

    // === Wizard State ===
    public SiteEvaluatorWizardState WizardState { get; set; } = new();
    
    public int CurrentStep { get; set; } = 1;
    public const int TotalSteps = 8;

    // === Step Configuration ===
    public List<WizardStepConfig> StepConfigs { get; set; } = [];

    // === Bound Properties ===
    
    [BindProperty]
    public string Address { get; set; } = "";

    [BindProperty]
    public string? TitleReference { get; set; }

    [BindProperty]
    public double? Latitude { get; set; }

    [BindProperty]
    public double? Longitude { get; set; }

    [BindProperty]
    public string SearchType { get; set; } = "address";

    [BindProperty]
    public string? SelectedPropertyIndex { get; set; }

    [BindProperty]
    public string? SelectedExistingId { get; set; }

    [BindProperty]
    public bool CreateNew { get; set; } = true;

    // === Intended Use Properties ===
    
    [BindProperty]
    public string IntendedUseCategory { get; set; } = "Residential";

    [BindProperty]
    public string? SpecificUse { get; set; }

    [BindProperty]
    public string EvaluationPurpose { get; set; } = "Purchase";

    [BindProperty]
    public bool IsNewDevelopment { get; set; }

    [BindProperty]
    public double? ProposedHeight { get; set; }

    [BindProperty]
    public double? ProposedCoverage { get; set; }

    [BindProperty]
    public int? ProposedUnits { get; set; }

    [BindProperty]
    public double? ProposedGfa { get; set; }

    [BindProperty]
    public string? IntendedUseNotes { get; set; }

    // === Customer Info Properties ===
    
    [BindProperty]
    public string? CustomerName { get; set; }

    [BindProperty]
    public string? CustomerCompany { get; set; }

    [BindProperty]
    public string? CustomerEmail { get; set; }

    [BindProperty]
    public string? CustomerReference { get; set; }

    // === Job Selection Properties ===

    [BindProperty]
    public string? SelectedJobId { get; set; }

    // === Messages ===
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int step = 1, string? id = null, string? jobId = null)
    {
        ViewData["Title"] = "Site Evaluation Wizard";
        ViewData["NoIndex"] = true;

        // Load existing wizard state
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        // If a job ID is provided, load that job and continue
        if (!string.IsNullOrEmpty(jobId))
        {
            var existingJob = await _jobService.GetJobAsync(jobId);
            if (existingJob != null)
            {
                WizardState.Job = existingJob;
                WizardState.Address = new AddressInput
                {
                    FullAddress = existingJob.Address,
                    SearchType = Models.Wizard.SearchType.ExistingEvaluation
                };
                
                // Load the location
                var location = await _locationService.GetLocationAsync(existingJob.LocationId);
                if (location != null)
                {
                    WizardState.Location = location;
                    WizardState.Address.Latitude = location.Latitude;
                    WizardState.Address.Longitude = location.Longitude;
                    
                    // Build evaluation from cached location data
                    WizardState.Evaluation = BuildEvaluationFromLocation(location, existingJob);
                }
                
                WizardState.PropertyMatch = new PropertyMatchResult
                {
                    CreateNew = false,
                    SelectedJobId = jobId
                };
                
                _logger.LogInformation("Loaded existing job {JobReference} for wizard", existingJob.JobReference);
            }
        }
        // Legacy: If an evaluation ID is provided, load that evaluation
        else if (!string.IsNullOrEmpty(id))
        {
            var existing = await _searchService.GetEvaluationAsync(id);
            if (existing != null)
            {
                WizardState.Evaluation = existing;
                WizardState.Address = new AddressInput
                {
                    FullAddress = existing.Location.Address,
                    Latitude = existing.Location.Latitude,
                    Longitude = existing.Location.Longitude,
                    SearchType = Models.Wizard.SearchType.ExistingEvaluation
                };
                WizardState.PropertyMatch = new PropertyMatchResult
                {
                    CreateNew = false,
                    SelectedExistingId = id
                };
            }
        }

        InitializeStepConfigs();
        CurrentStep = Math.Clamp(step, 1, TotalSteps);
        WizardState.CurrentStep = CurrentStep;
        
        // Pre-populate form from wizard state
        if (WizardState.Address != null)
        {
            Address = WizardState.Address.FullAddress;
            TitleReference = WizardState.Address.TitleReference;
            Latitude = WizardState.Address.Latitude;
            Longitude = WizardState.Address.Longitude;
            SearchType = WizardState.Address.SearchType.ToString().ToLowerInvariant();
        }
        
        // Pre-populate customer info
        if (WizardState.CustomerInfo != null)
        {
            CustomerName = WizardState.CustomerInfo.CustomerName;
            CustomerCompany = WizardState.CustomerInfo.CustomerCompany;
            CustomerEmail = WizardState.CustomerInfo.CustomerEmail;
            CustomerReference = WizardState.CustomerInfo.CustomerReference;
        }

        // Save state back
        TempData.SetSiteEvaluatorWizardState(WizardState);

        return Page();
    }

    /// <summary>
    /// Build a SiteEvaluation from cached location data (for display purposes).
    /// </summary>
    private static SiteEvaluation BuildEvaluationFromLocation(PropertyLocation location, EvaluationJob job)
    {
        return new SiteEvaluation
        {
            Id = job.Id,
            UserId = job.CreatedByUserId,
            CreatedDate = job.CreatedDate,
            LastUpdated = job.LastUpdated,
            Location = new SiteLocation
            {
                Address = location.Address,
                LegalDescription = location.LegalDescription ?? string.Empty,
                TitleReference = location.TitleReference,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Suburb = location.Suburb,
                City = location.City,
                TerritorialAuthority = location.TerritorialAuthority,
                RegionalCouncil = location.RegionalCouncil,
                Boundary = location.Boundary
            },
            Zoning = location.CachedZoning,
            Hazards = location.CachedHazards,
            Geotech = location.CachedGeotech,
            Infrastructure = location.CachedInfrastructure,
            Climate = location.CachedClimate,
            Land = location.CachedLand,
            Status = job.Status == JobStatus.Complete ? EvaluationStatus.Complete : EvaluationStatus.InProgress
        };
    }

    /// <summary>
    /// Step 1: Save address and search for property
    /// </summary>
    public async Task<IActionResult> OnPostSearchAddressAsync()
    {
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(Address))
            {
                ErrorMessage = "Please enter an address to search.";
                CurrentStep = 1;
                InitializeStepConfigs();
                return Page();
            }

            // Parse intended use category
            var useCategory = IntendedUseCategory switch
            {
                "Commercial" => PropertyUseCategory.Commercial,
                "Industrial" => PropertyUseCategory.Industrial,
                "MixedUse" => PropertyUseCategory.MixedUse,
                "Rural" => PropertyUseCategory.Rural,
                "Community" => PropertyUseCategory.Community,
                "OpenSpace" => PropertyUseCategory.OpenSpace,
                "Other" => PropertyUseCategory.Other,
                _ => PropertyUseCategory.Residential
            };

            // Parse evaluation purpose
            var purpose = EvaluationPurpose switch
            {
                "Sale" => Models.Wizard.EvaluationPurpose.Sale,
                "Development" => Models.Wizard.EvaluationPurpose.Development,
                "Subdivision" => Models.Wizard.EvaluationPurpose.Subdivision,
                "DueDiligence" => Models.Wizard.EvaluationPurpose.DueDiligence,
                "ResourceConsent" => Models.Wizard.EvaluationPurpose.ResourceConsent,
                "Insurance" => Models.Wizard.EvaluationPurpose.Insurance,
                "Valuation" => Models.Wizard.EvaluationPurpose.Valuation,
                "Other" => Models.Wizard.EvaluationPurpose.Other,
                _ => Models.Wizard.EvaluationPurpose.Purchase
            };

            // Save address and intended use to state
            WizardState.Address = new AddressInput
            {
                FullAddress = Address.Trim(),
                TitleReference = TitleReference?.Trim(),
                Latitude = Latitude,
                Longitude = Longitude,
                SearchType = SearchType.ToLowerInvariant() switch
                {
                    "title" => Models.Wizard.SearchType.Title,
                    "coordinates" => Models.Wizard.SearchType.Coordinates,
                    _ => Models.Wizard.SearchType.Address
                },
                IntendedUse = new IntendedPropertyUse
                {
                    Category = useCategory,
                    SpecificUse = SpecificUse?.Trim(),
                    Purpose = purpose,
                    IsNewDevelopment = IsNewDevelopment,
                    ProposedHeight = ProposedHeight,
                    ProposedCoverage = ProposedCoverage,
                    ProposedUnits = ProposedUnits,
                    ProposedGfa = ProposedGfa,
                    Notes = IntendedUseNotes?.Trim()
                }
            };

            // Save customer info
            WizardState.CustomerInfo = new JobCustomerInfo
            {
                CustomerName = CustomerName?.Trim(),
                CustomerCompany = CustomerCompany?.Trim(),
                CustomerEmail = CustomerEmail?.Trim(),
                CustomerReference = CustomerReference?.Trim()
            };

            // Step 2: Search for property matches
            var matchResult = new PropertyMatchResult();

            // Search for existing jobs at this address
            var existingJobs = await _jobService.SearchJobsAsync(Address.Split(',')[0]);
            foreach (var job in existingJobs.Take(10))
            {
                matchResult.ExistingJobs.Add(new ExistingJobMatch
                {
                    Id = job.Id,
                    JobReference = job.JobReference,
                    Address = job.Address,
                    CustomerName = job.CustomerName,
                    CustomerCompany = job.CustomerCompany,
                    Purpose = job.Purpose,
                    Status = job.Status,
                    CreatedDate = job.CreatedDate,
                    LastUpdated = job.LastUpdated,
                    CompletenessPercent = job.CompletenessPercent,
                    ReportCount = job.Reports.Count
                });
            }

            // Check for existing evaluations (legacy support)
            var existingEvaluations = await _repository.FindAsync<SiteEvaluation>(e => 
                e.Location != null && 
                e.Location.Address != null && 
                e.Location.Address.Contains(Address.Split(',')[0]));

            foreach (var existing in existingEvaluations.Take(5))
            {
                matchResult.ExistingEvaluations.Add(new ExistingEvaluationMatch
                {
                    Id = existing.Id,
                    Address = existing.Location.Address,
                    CreatedDate = existing.CreatedDate,
                    LastUpdated = existing.LastUpdated,
                    CompletenessPercent = (int)(existing.Completeness?.CompletionPercentage ?? 0),
                    Status = existing.Status
                });
            }

            // Get LINZ address suggestions
            var linzSuggestions = await _linzService.GetAddressSuggestionsAsync(Address);
            foreach (var suggestion in linzSuggestions.Take(10))
            {
                matchResult.LinzMatches.Add(new LinzPropertyMatch
                {
                    FullAddress = suggestion.FullAddress,
                    Suburb = suggestion.Suburb,
                    City = suggestion.City,
                    Latitude = suggestion.Latitude ?? 0,
                    Longitude = suggestion.Longitude ?? 0,
                    MatchConfidence = 100 - (matchResult.LinzMatches.Count * 5) // Simple confidence scoring
                });
            }

            // Auto-select if only one high-confidence match and no existing jobs
            if (matchResult.LinzMatches.Count == 1 && 
                matchResult.ExistingJobs.Count == 0 && 
                matchResult.ExistingEvaluations.Count == 0)
            {
                matchResult.SelectedProperty = matchResult.LinzMatches[0];
                matchResult.CreateNew = true;
            }

            WizardState.PropertyMatch = matchResult;
            WizardState.CurrentStep = 2;
            TempData.SetSiteEvaluatorWizardState(WizardState);

            _logger.LogInformation("Address search completed: {Address}, Use: {UseCategory}/{Purpose}, {LinzMatches} LINZ matches, {ExistingJobs} existing jobs", 
                Address, useCategory, purpose, matchResult.LinzMatches.Count, matchResult.ExistingJobs.Count);

            return RedirectToPage(new { step = 2 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching address: {Address}", Address);
            ErrorMessage = $"Error searching address: {ex.Message}";
            CurrentStep = 1;
            InitializeStepConfigs();
            TempData.SetSiteEvaluatorWizardState(WizardState);
            return Page();
        }
    }

    /// <summary>
    /// Step 2: Select property and start evaluation
    /// </summary>
    public async Task<IActionResult> OnPostSelectPropertyAsync()
    {
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        try
        {
            // Check if continuing an existing job
            if (!CreateNew && !string.IsNullOrEmpty(SelectedJobId))
            {
                var existingJob = await _jobService.GetJobAsync(SelectedJobId);
                if (existingJob != null)
                {
                    WizardState.Job = existingJob;
                    WizardState.PropertyMatch!.CreateNew = false;
                    WizardState.PropertyMatch.SelectedJobId = SelectedJobId;
                    
                    // Load location and build evaluation for display
                    var location = await _locationService.GetLocationAsync(existingJob.LocationId);
                    if (location != null)
                    {
                        WizardState.Location = location;
                        WizardState.Evaluation = BuildEvaluationFromLocation(location, existingJob);
                    }
                    
                    WizardState.CurrentStep = 3;
                    TempData.SetSiteEvaluatorWizardState(WizardState);
                    
                    _logger.LogInformation("Continuing existing job: {JobReference}", existingJob.JobReference);
                    return RedirectToPage(new { step = 3 });
                }
            }

            // Check if using existing evaluation (legacy)
            if (!CreateNew && !string.IsNullOrEmpty(SelectedExistingId))
            {
                var existing = await _searchService.GetEvaluationAsync(SelectedExistingId);
                if (existing != null)
                {
                    WizardState.Evaluation = existing;
                    WizardState.PropertyMatch!.CreateNew = false;
                    WizardState.PropertyMatch.SelectedExistingId = SelectedExistingId;
                    WizardState.CurrentStep = 3;
                    TempData.SetSiteEvaluatorWizardState(WizardState);
                    
                    return RedirectToPage(new { step = 3 });
                }
            }

            // Create a new job
            string selectedAddress;
            if (int.TryParse(SelectedPropertyIndex, out var index) && 
                WizardState.PropertyMatch?.LinzMatches != null &&
                index >= 0 && index < WizardState.PropertyMatch.LinzMatches.Count)
            {
                var selectedProperty = WizardState.PropertyMatch.LinzMatches[index];
                WizardState.PropertyMatch.SelectedProperty = selectedProperty;
                selectedAddress = selectedProperty.FullAddress;
            }
            else if (WizardState.PropertyMatch?.SelectedProperty != null)
            {
                selectedAddress = WizardState.PropertyMatch.SelectedProperty.FullAddress;
            }
            else
            {
                selectedAddress = WizardState.Address.FullAddress;
            }

            // Map wizard purpose to job purpose
            var jobPurpose = WizardState.Address.IntendedUse.Purpose switch
            {
                Models.Wizard.EvaluationPurpose.Sale => JobPurpose.Sale,
                Models.Wizard.EvaluationPurpose.Development => JobPurpose.Development,
                Models.Wizard.EvaluationPurpose.Subdivision => JobPurpose.Subdivision,
                Models.Wizard.EvaluationPurpose.DueDiligence => JobPurpose.DueDiligence,
                Models.Wizard.EvaluationPurpose.ResourceConsent => JobPurpose.ResourceConsent,
                Models.Wizard.EvaluationPurpose.Insurance => JobPurpose.Insurance,
                Models.Wizard.EvaluationPurpose.Valuation => JobPurpose.Valuation,
                _ => JobPurpose.Purchase
            };

            // Create the job
            var createRequest = new CreateJobRequest
            {
                Address = selectedAddress,
                Title = $"Evaluation - {selectedAddress}",
                CustomerName = WizardState.CustomerInfo.CustomerName,
                CustomerCompany = WizardState.CustomerInfo.CustomerCompany,
                CustomerEmail = WizardState.CustomerInfo.CustomerEmail,
                CustomerReference = WizardState.CustomerInfo.CustomerReference,
                Purpose = jobPurpose,
                IntendedUse = WizardState.Address.IntendedUse.Category,
                IntendedUseDetails = WizardState.Address.IntendedUse.SpecificUse,
                IsNewDevelopment = WizardState.Address.IntendedUse.IsNewDevelopment,
                ProposedHeight = WizardState.Address.IntendedUse.ProposedHeight,
                ProposedCoverage = WizardState.Address.IntendedUse.ProposedCoverage,
                ProposedUnits = WizardState.Address.IntendedUse.ProposedUnits,
                ProposedGfa = WizardState.Address.IntendedUse.ProposedGfa,
                InternalNotes = WizardState.Address.IntendedUse.Notes,
                AutoStartDataCollection = true
            };

            var newJob = await _jobService.CreateJobAsync(createRequest);
            WizardState.Job = newJob;
            WizardState.PropertyMatch!.CreateNew = true;

            // Load the location created for the job
            var newLocation = await _locationService.GetLocationAsync(newJob.LocationId);
            if (newLocation != null)
            {
                WizardState.Location = newLocation;
                WizardState.Evaluation = BuildEvaluationFromLocation(newLocation, newJob);
            }

            _logger.LogInformation("Created new job: {JobReference} for {Address}", 
                newJob.JobReference, selectedAddress);

            WizardState.CurrentStep = 3;
            TempData.SetSiteEvaluatorWizardState(WizardState);

            return RedirectToPage(new { step = 3 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting property");
            ErrorMessage = $"Error selecting property: {ex.Message}";
            CurrentStep = 2;
            InitializeStepConfigs();
            TempData.SetSiteEvaluatorWizardState(WizardState);
            return Page();
        }
    }

    /// <summary>
    /// Step 3-7: Navigate through data review steps
    /// </summary>
    public async Task<IActionResult> OnPostNextStepAsync(int currentStep)
    {
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        var nextStep = Math.Min(currentStep + 1, TotalSteps);
        WizardState.CurrentStep = nextStep;
        TempData.SetSiteEvaluatorWizardState(WizardState);

        return RedirectToPage(new { step = nextStep });
    }

    /// <summary>
    /// Navigate to previous step
    /// </summary>
    public async Task<IActionResult> OnPostPreviousStepAsync(int currentStep)
    {
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        var prevStep = Math.Max(currentStep - 1, 1);
        WizardState.CurrentStep = prevStep;
        TempData.SetSiteEvaluatorWizardState(WizardState);

        return RedirectToPage(new { step = prevStep });
    }

    /// <summary>
    /// Step 8: Complete wizard and generate report
    /// </summary>
    public async Task<IActionResult> OnPostCompleteWizardAsync()
    {
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        try
        {
            // Complete the job if using new architecture
            if (WizardState.Job != null)
            {
                await _jobService.UpdateJobStatusAsync(WizardState.Job.Id, JobStatus.Complete);
                WizardState.Job.Status = JobStatus.Complete;
                
                _logger.LogInformation("Job completed: {JobReference}", WizardState.Job.JobReference);
            }
            
            // Legacy: Update evaluation status
            if (WizardState.Evaluation != null)
            {
                WizardState.Evaluation.Status = EvaluationStatus.Complete;
                WizardState.Evaluation.LastUpdated = DateTime.UtcNow;
                await _repository.UpdateAsync(WizardState.Evaluation);
            }

            WizardState.IsCompleted = true;
            TempData.SetSiteEvaluatorWizardState(WizardState);

            _logger.LogInformation("Wizard completed for job: {JobRef}, evaluation: {Id}", 
                WizardState.Job?.JobReference, WizardState.Evaluation?.Id);

            // Redirect to results page - prefer job-based URL
            if (WizardState.Job != null)
            {
                return RedirectToPage("/SiteEvaluator/JobDetails", new { jobId = WizardState.Job.Id });
            }
            
            return RedirectToPage("/SiteEvaluator/Search", new { id = WizardState.Evaluation?.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing wizard");
            ErrorMessage = $"Error completing wizard: {ex.Message}";
            CurrentStep = TotalSteps;
            InitializeStepConfigs();
            TempData.SetSiteEvaluatorWizardState(WizardState);
            return Page();
        }
    }

    /// <summary>
    /// Refresh data for a specific section
    /// </summary>
    public async Task<IActionResult> OnPostRefreshSectionAsync(string section, int currentStep)
    {
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        try
        {
            if (WizardState.Evaluation != null)
            {
                var refreshed = await _searchService.RefreshDataAsync(
                    WizardState.Evaluation.Id, 
                    new[] { section });
                WizardState.Evaluation = refreshed;
            }

            TempData.SetSiteEvaluatorWizardState(WizardState);
            SuccessMessage = $"? {section} data refreshed successfully!";

            return RedirectToPage(new { step = currentStep });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing section: {Section}", section);
            ErrorMessage = $"Error refreshing {section}: {ex.Message}";
            CurrentStep = currentStep;
            InitializeStepConfigs();
            TempData.SetSiteEvaluatorWizardState(WizardState);
            return Page();
        }
    }

    /// <summary>
    /// Start fresh - clear wizard state
    /// </summary>
    public IActionResult OnPostStartFresh()
    {
        TempData.ClearSiteEvaluatorWizardState();
        return RedirectToPage(new { step = 1 });
    }

    private void InitializeStepConfigs()
    {
        StepConfigs =
        [
            new WizardStepConfig
            {
                StepNumber = 1,
                Label = "Address",
                Icon = "fa-map-marker-alt",
                Anchor = "step-1-address",
                Handler = "SearchAddress",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 2,
                Label = "Property",
                Icon = "fa-building",
                Anchor = "step-2-property",
                Handler = "SelectProperty",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 3,
                Label = "Zoning",
                Icon = "fa-city",
                Anchor = "step-3-zoning",
                Handler = "NextStep",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 4,
                Label = "Hazards",
                Icon = "fa-exclamation-triangle",
                Anchor = "step-4-hazards",
                Handler = "NextStep",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 5,
                Label = "Geotech",
                Icon = "fa-layer-group",
                Anchor = "step-5-geotech",
                Handler = "NextStep",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 6,
                Label = "Infrastructure",
                Icon = "fa-plug",
                Anchor = "step-6-infrastructure",
                Handler = "NextStep",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 7,
                Label = "Climate",
                Icon = "fa-cloud-sun-rain",
                Anchor = "step-7-climate",
                Handler = "NextStep",
                IsEnabled = true
            },
            new WizardStepConfig
            {
                StepNumber = 8,
                Label = "Summary",
                Icon = "fa-check-circle",
                Anchor = "step-8-summary",
                Handler = "CompleteWizard",
                IsEnabled = true
            }
        ];
    }

    public WizardStepConfig? GetStepConfig(int stepNumber) =>
        StepConfigs.FirstOrDefault(s => s.StepNumber == stepNumber);

    public int GetNextStepNumber(int current) => Math.Min(current + 1, TotalSteps);
    public int GetPreviousStepNumber(int current) => Math.Max(current - 1, 1);
}

/// <summary>
/// Configuration for a wizard step.
/// </summary>
public class WizardStepConfig
{
    public int StepNumber { get; set; }
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "fa-circle";
    public string Anchor { get; set; } = "";
    public string Handler { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}
