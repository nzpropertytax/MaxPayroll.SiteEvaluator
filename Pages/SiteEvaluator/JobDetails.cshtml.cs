using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaxPayroll.SiteEvaluator.Pages.SiteEvaluator;

/// <summary>
/// View and manage a specific evaluation job.
/// </summary>
public class JobDetailsModel : PageModel
{
    private readonly IJobService _jobService;
    private readonly ILocationService _locationService;
    private readonly ILogger<JobDetailsModel> _logger;

    public JobDetailsModel(
        IJobService jobService,
        ILocationService locationService,
        ILogger<JobDetailsModel> logger)
    {
        _jobService = jobService;
        _locationService = locationService;
        _logger = logger;
    }

    // === View Data ===
    
    public EvaluationJob? Job { get; set; }
    public PropertyLocation? Location { get; set; }
    public SiteEvaluation? Evaluation { get; set; }

    // === Form Properties ===
    
    [BindProperty]
    public string? CustomerName { get; set; }

    [BindProperty]
    public string? CustomerCompany { get; set; }

    [BindProperty]
    public string? CustomerEmail { get; set; }

    [BindProperty]
    public string? CustomerReference { get; set; }

    [BindProperty]
    public string? InternalNotes { get; set; }

    // === Messages ===
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
        {
            return RedirectToPage("/SiteEvaluator/Dashboard");
        }

        try
        {
            Job = await _jobService.GetJobAsync(jobId);
            if (Job == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Job {Job.JobReference}";

            // Load location and build evaluation
            Location = await _locationService.GetLocationAsync(Job.LocationId);
            if (Location != null)
            {
                Evaluation = BuildEvaluationFromLocation(Location, Job);
            }

            // Pre-populate form fields
            CustomerName = Job.CustomerName;
            CustomerCompany = Job.CustomerCompany;
            CustomerEmail = Job.CustomerEmail;
            CustomerReference = Job.CustomerReference;
            InternalNotes = Job.InternalNotes;

            // Check for messages
            if (TempData.TryGetValue("SuccessMessage", out var successMsg))
            {
                SuccessMessage = successMsg?.ToString();
            }
            if (TempData.TryGetValue("ErrorMessage", out var errorMsg))
            {
                ErrorMessage = errorMsg?.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading job {JobId}", jobId);
            ErrorMessage = $"Error loading job: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(string jobId)
    {
        try
        {
            var updateRequest = new UpdateJobRequest
            {
                CustomerName = CustomerName,
                CustomerCompany = CustomerCompany,
                CustomerEmail = CustomerEmail,
                CustomerReference = CustomerReference,
                InternalNotes = InternalNotes
            };

            await _jobService.UpdateJobAsync(jobId, updateRequest);
            TempData["SuccessMessage"] = "Job updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {JobId}", jobId);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnPostRefreshDataAsync(string jobId, string section)
    {
        try
        {
            var sections = string.IsNullOrEmpty(section) 
                ? new[] { "zoning", "hazards", "geotech", "infrastructure", "climate", "land" }
                : new[] { section };

            await _jobService.RefreshDataSectionsAsync(jobId, sections);
            TempData["SuccessMessage"] = $"Data refreshed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing data for job {JobId}", jobId);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnPostGenerateReportAsync(string jobId, string reportType)
    {
        try
        {
            var type = reportType switch
            {
                "summary" => ReportType.SummaryReport,
                "geotech" => ReportType.GeotechBrief,
                "duediligence" => ReportType.DueDiligencePack,
                _ => ReportType.FullReport
            };

            var options = new ReportOptions
            {
                PreparedFor = CustomerName ?? CustomerCompany ?? "Client",
                IncludeMaps = true,
                IncludeAppendices = true
            };

            var report = await _jobService.GenerateReportAsync(jobId, type, options);
            TempData["SuccessMessage"] = $"Report '{report.Title}' generated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for job {JobId}", jobId);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnPostCompleteJobAsync(string jobId)
    {
        try
        {
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.Complete);
            TempData["SuccessMessage"] = "Job marked as complete.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing job {JobId}", jobId);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnGetDownloadReportAsync(string jobId, string reportId)
    {
        try
        {
            var content = await _jobService.GetReportContentAsync(jobId, reportId);
            if (content == null)
            {
                return NotFound();
            }

            var job = await _jobService.GetJobAsync(jobId);
            var report = job?.Reports.FirstOrDefault(r => r.Id == reportId);
            var fileName = report?.FileName ?? $"report-{reportId}.pdf";

            return File(content, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report {ReportId}", reportId);
            return NotFound();
        }
    }

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
}
