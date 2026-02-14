using MaxPayroll.SiteEvaluator.Configuration;
using MaxPayroll.SiteEvaluator.Services;
using MaxPayroll.SiteEvaluator.Services.Integration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace MaxPayroll.SiteEvaluator.Pages.SiteEvaluator.Admin;

/// <summary>
/// Admin settings page for Site Evaluator configuration.
/// SuperAdmin only - manages API keys, service configuration, and system settings.
/// </summary>
[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly IOptions<SiteEvaluatorOptions> _options;
    private readonly ILinzDataService _linzService;
    private readonly INzgdDataService _nzgdService;
    private readonly IGnsDataService _gnsService;
    private readonly INiwaDataService _niwaService;
    private readonly IEnumerable<ICouncilDataService> _councilServices;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        IOptions<SiteEvaluatorOptions> options,
        ILinzDataService linzService,
        INzgdDataService nzgdService,
        IGnsDataService gnsService,
        INiwaDataService niwaService,
        IEnumerable<ICouncilDataService> councilServices,
        ILogger<SettingsModel> logger)
    {
        _options = options;
        _linzService = linzService;
        _nzgdService = nzgdService;
        _gnsService = gnsService;
        _niwaService = niwaService;
        _councilServices = councilServices;
        _logger = logger;
    }

    // === View Properties ===
    
    public SiteEvaluatorOptions Options => _options.Value;
    public List<ServiceStatusInfo> ServiceStatuses { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // === Input Properties ===
    
    [BindProperty]
    public string LinzApiKey { get; set; } = "";

    [BindProperty]
    public string NzgdApiKey { get; set; } = "";

    [BindProperty]
    public string StripeSecretKey { get; set; } = "";

    [BindProperty]
    public string StripeWebhookSecret { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Site Evaluator Settings";
        ViewData["NoIndex"] = true;

        // Check if user is SuperAdmin
        if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Mask API keys for display
        LinzApiKey = MaskApiKey(Options.Linz.ApiKey);
        NzgdApiKey = MaskApiKey(Options.Nzgd.ApiKey);
        StripeSecretKey = MaskApiKey(Options.Stripe.SecretKey);
        StripeWebhookSecret = MaskApiKey(Options.Stripe.WebhookSecret);

        // Check service statuses
        await CheckServiceStatusesAsync();

        // Check for messages
        if (TempData.TryGetValue("SuccessMessage", out var successMsg))
        {
            SuccessMessage = successMsg?.ToString();
        }
        if (TempData.TryGetValue("ErrorMessage", out var errorMsg))
        {
            ErrorMessage = errorMsg?.ToString();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostTestLinzAsync()
    {
        try
        {
            // Test LINZ connection with a simple query
            var results = await _linzService.GetAddressSuggestionsAsync("1 Willis Street Wellington");
            
            if (results.Any())
            {
                TempData["SuccessMessage"] = $"? LINZ API connected! Found {results.Count()} address suggestions.";
            }
            else
            {
                TempData["ErrorMessage"] = "?? LINZ API connected but returned no results. Check API key permissions.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINZ API test failed");
            TempData["ErrorMessage"] = $"? LINZ API test failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestNzgdAsync()
    {
        try
        {
            // Test NZGD connection with a coordinates search
            var results = await _nzgdService.GetNearbyBoreholesAsync(-43.532, 172.636, 500);
            TempData["SuccessMessage"] = $"? NZGD service responded! Found {results.Count()} boreholes near Christchurch CBD.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NZGD API test failed");
            TempData["ErrorMessage"] = $"? NZGD test failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestGnsAsync()
    {
        try
        {
            // Test GNS with seismic data lookup
            var seismic = await _gnsService.GetSeismicHazardAsync(-43.532, 172.636);
            if (seismic != null)
            {
                TempData["SuccessMessage"] = $"? GNS service responded! Z-factor for Christchurch: {seismic.ZoneFactor}";
            }
            else
            {
                TempData["ErrorMessage"] = "?? GNS service connected but returned no seismic data.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GNS API test failed");
            TempData["ErrorMessage"] = $"? GNS test failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestNiwaAsync()
    {
        try
        {
            // Test NIWA with rainfall data lookup
            var rainfall = await _niwaService.GetRainfallDataAsync(-43.532, 172.636);
            if (rainfall != null)
            {
                TempData["SuccessMessage"] = $"? NIWA service responded! Annual rainfall: {rainfall.AnnualMean}mm";
            }
            else
            {
                // Try wind zone as fallback
                var windZone = await _niwaService.GetWindZoneAsync(-43.532, 172.636);
                if (!string.IsNullOrEmpty(windZone))
                {
                    TempData["SuccessMessage"] = $"? NIWA service responded! Wind zone: {windZone}";
                }
                else
                {
                    TempData["ErrorMessage"] = "?? NIWA service connected but returned no climate data.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NIWA API test failed");
            TempData["ErrorMessage"] = $"? NIWA test failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestCouncilAsync(string council)
    {
        try
        {
            var councilService = council.ToLower() switch
            {
                "christchurch" => _councilServices.FirstOrDefault(c => c.GetType().Name.Contains("Christchurch")),
                "auckland" => _councilServices.FirstOrDefault(c => c.GetType().Name.Contains("Auckland")),
                "wellington" => _councilServices.FirstOrDefault(c => c.GetType().Name.Contains("Wellington")),
                _ => null
            };

            if (councilService == null)
            {
                TempData["ErrorMessage"] = $"Council service '{council}' not found.";
                return RedirectToPage();
            }

            // Test with known coordinates for each council
            var (lat, lon) = council.ToLower() switch
            {
                "christchurch" => (-43.532, 172.636),
                "auckland" => (-36.848, 174.762),
                "wellington" => (-41.286, 174.776),
                _ => (0.0, 0.0)
            };

            var canHandle = councilService.SupportsRegion(lat, lon);
            
            if (canHandle)
            {
                var zoning = await councilService.GetZoningDataAsync(lat, lon);
                TempData["SuccessMessage"] = $"? {council} Council GIS responded! Zone: {zoning?.Zone ?? "Unknown"}";
            }
            else
            {
                TempData["ErrorMessage"] = $"?? {council} Council service cannot handle test coordinates.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Council} Council test failed", council);
            TempData["ErrorMessage"] = $"? {council} Council test failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task CheckServiceStatusesAsync()
    {
        ServiceStatuses = new List<ServiceStatusInfo>
        {
            new ServiceStatusInfo
            {
                Name = "LINZ Address API",
                Description = "Address geocoding and autocomplete",
                IsConfigured = !string.IsNullOrEmpty(Options.Linz.ApiKey),
                RequiresApiKey = true,
                ApiKeyName = "SiteEvaluator:Linz:ApiKey",
                DocumentationUrl = "https://data.linz.govt.nz/",
                Status = !string.IsNullOrEmpty(Options.Linz.ApiKey) ? ServiceStatus.Configured : ServiceStatus.NotConfigured
            },
            new ServiceStatusInfo
            {
                Name = "LINZ Landonline",
                Description = "Property title and ownership data",
                IsConfigured = false, // Requires paid subscription
                RequiresApiKey = true,
                RequiresPaidSubscription = true,
                EstimatedCost = "$500/year",
                ApiKeyName = "SiteEvaluator:Linz:LandonlineApiKey",
                DocumentationUrl = "https://www.linz.govt.nz/products-services/data/types-linz-data/property-ownership-and-boundary-data",
                Status = ServiceStatus.FallbackMode,
                FallbackDescription = "Returns estimated title type and district from reference format"
            },
            new ServiceStatusInfo
            {
                Name = "NZGD (Geotechnical)",
                Description = "Borehole, CPT, and geotechnical report data",
                IsConfigured = true, // No API key needed for public data
                RequiresApiKey = false,
                DocumentationUrl = "https://www.nzgd.org.nz/",
                Status = ServiceStatus.Configured,
                FallbackDescription = "Returns regional soil profiles when API unavailable"
            },
            new ServiceStatusInfo
            {
                Name = "GNS Science (Seismic)",
                Description = "Seismic hazard, fault data, and PGA values",
                IsConfigured = true, // Uses static data + GeoNet
                RequiresApiKey = false,
                DocumentationUrl = "https://www.gns.cri.nz/",
                Status = ServiceStatus.Configured,
                FallbackDescription = "NZS 1170.5 Z-values and major fault database always available"
            },
            new ServiceStatusInfo
            {
                Name = "NIWA (Climate)",
                Description = "Rainfall (HIRDS), wind zones, temperature data",
                IsConfigured = true, // Uses static regional data
                RequiresApiKey = false,
                DocumentationUrl = "https://hirds.niwa.co.nz/",
                Status = ServiceStatus.Configured,
                FallbackDescription = "Full HIRDS rainfall tables with climate change factors"
            },
            new ServiceStatusInfo
            {
                Name = "Christchurch City Council",
                Description = "CCC GIS - zoning, hazards, infrastructure",
                IsConfigured = true,
                RequiresApiKey = false,
                DocumentationUrl = "https://opendata.canterburymaps.govt.nz/",
                Status = ServiceStatus.Configured
            },
            new ServiceStatusInfo
            {
                Name = "Auckland Council",
                Description = "Auckland GIS - zoning, hazards, infrastructure",
                IsConfigured = true,
                RequiresApiKey = false,
                DocumentationUrl = "https://data.aucklandcouncil.govt.nz/",
                Status = ServiceStatus.Configured
            },
            new ServiceStatusInfo
            {
                Name = "Wellington City Council",
                Description = "WCC GIS - zoning, hazards, seismic",
                IsConfigured = true,
                RequiresApiKey = false,
                DocumentationUrl = "https://data.wcc.govt.nz/",
                Status = ServiceStatus.Configured
            },
            new ServiceStatusInfo
            {
                Name = "Stripe (Billing)",
                Description = "Payment processing for subscriptions",
                IsConfigured = !string.IsNullOrEmpty(Options.Stripe.SecretKey),
                RequiresApiKey = true,
                ApiKeyName = "SiteEvaluator:Stripe:SecretKey",
                DocumentationUrl = "https://stripe.com/docs",
                Status = !string.IsNullOrEmpty(Options.Stripe.SecretKey) ? ServiceStatus.Configured : ServiceStatus.NotConfigured
            }
        };

        await Task.CompletedTask;
    }

    private static string MaskApiKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.Length <= 8) return "****";
        return key.Substring(0, 4) + "****" + key.Substring(key.Length - 4);
    }
}

public class ServiceStatusInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsConfigured { get; set; }
    public bool RequiresApiKey { get; set; }
    public bool RequiresPaidSubscription { get; set; }
    public string? EstimatedCost { get; set; }
    public string? ApiKeyName { get; set; }
    public string? DocumentationUrl { get; set; }
    public ServiceStatus Status { get; set; }
    public string? FallbackDescription { get; set; }
}

public enum ServiceStatus
{
    Configured,
    NotConfigured,
    FallbackMode,
    Error
}
