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
/// Steps:
/// 1. Address Entry - Enter address, title, or coordinates
/// 2. Property Match - Match to existing evaluations or select from LINZ results
/// 3. Zoning - Review zoning and planning data
/// 4. Hazards - Review natural hazards (flood, liquefaction, seismic)
/// 5. Geotech - Review nearby geotechnical data
/// 6. Infrastructure - Review 3 Waters, power, communications
/// 7. Climate - Review wind zones, rainfall, climate data
/// 8. Summary - Complete summary and report generation
/// </summary>
public class EvaluationWizardModel : PageModel
{
    private readonly ISiteSearchService _searchService;
    private readonly ILinzDataService _linzService;
    private readonly ISiteEvaluatorRepository _repository;
    private readonly ILogger<EvaluationWizardModel> _logger;

    public EvaluationWizardModel(
        ISiteSearchService searchService,
        ILinzDataService linzService,
        ISiteEvaluatorRepository repository,
        ILogger<EvaluationWizardModel> logger)
    {
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

    // === Messages ===
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int step = 1, string? id = null)
    {
        ViewData["Title"] = "Site Evaluation Wizard";
        ViewData["NoIndex"] = true;

        // Load existing wizard state
        WizardState = TempData.GetSiteEvaluatorWizardState();
        
        // If an ID is provided, load that evaluation
        if (!string.IsNullOrEmpty(id))
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

        // Save state back
        TempData.SetSiteEvaluatorWizardState(WizardState);

        return Page();
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

            // Step 2: Search for property matches
            var matchResult = new PropertyMatchResult();

            // Check for existing evaluations with similar address
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

            // Auto-select if only one high-confidence match
            if (matchResult.LinzMatches.Count == 1 && matchResult.ExistingEvaluations.Count == 0)
            {
                matchResult.SelectedProperty = matchResult.LinzMatches[0];
                matchResult.CreateNew = true;
            }

            WizardState.PropertyMatch = matchResult;
            WizardState.CurrentStep = 2;
            TempData.SetSiteEvaluatorWizardState(WizardState);

            _logger.LogInformation("Address search completed: {Address}, Use: {UseCategory}/{Purpose}, {LinzMatches} LINZ matches, {ExistingMatches} existing", 
                Address, useCategory, purpose, matchResult.LinzMatches.Count, matchResult.ExistingEvaluations.Count);

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
            // Check if using existing evaluation
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

            // Create new evaluation from selected property
            if (int.TryParse(SelectedPropertyIndex, out var index) && 
                WizardState.PropertyMatch?.LinzMatches != null &&
                index >= 0 && index < WizardState.PropertyMatch.LinzMatches.Count)
            {
                var selectedProperty = WizardState.PropertyMatch.LinzMatches[index];
                WizardState.PropertyMatch.SelectedProperty = selectedProperty;
                WizardState.PropertyMatch.CreateNew = true;

                // Run the full evaluation
                var evaluation = await _searchService.SearchByAddressAsync(selectedProperty.FullAddress);
                WizardState.Evaluation = evaluation;
                
                _logger.LogInformation("Created new evaluation: {Id} for {Address}", 
                    evaluation.Id, selectedProperty.FullAddress);
            }
            else if (WizardState.PropertyMatch?.SelectedProperty != null)
            {
                // Use pre-selected property
                var evaluation = await _searchService.SearchByAddressAsync(
                    WizardState.PropertyMatch.SelectedProperty.FullAddress);
                WizardState.Evaluation = evaluation;
            }
            else
            {
                // Fall back to original address search
                var evaluation = await _searchService.SearchByAddressAsync(WizardState.Address.FullAddress);
                WizardState.Evaluation = evaluation;
            }

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
            if (WizardState.Evaluation != null)
            {
                WizardState.Evaluation.Status = EvaluationStatus.Complete;
                WizardState.Evaluation.LastUpdated = DateTime.UtcNow;
                await _repository.UpdateAsync(WizardState.Evaluation);
            }

            WizardState.IsCompleted = true;
            TempData.SetSiteEvaluatorWizardState(WizardState);

            _logger.LogInformation("Wizard completed for evaluation: {Id}", WizardState.Evaluation?.Id);

            // Redirect to results page
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
