using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaxPayroll.SiteEvaluator.Pages.SiteEvaluator;

/// <summary>
/// User dashboard showing job history, statistics, and quick actions.
/// </summary>
public class DashboardModel : PageModel
{
    private readonly IJobService _jobService;
    private readonly ILocationService _locationService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(
        IJobService jobService,
        ILocationService locationService,
        ISubscriptionService subscriptionService,
        ILogger<DashboardModel> logger)
    {
        _jobService = jobService;
        _locationService = locationService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    // === View Data ===
    
    public List<EvaluationJob> RecentJobs { get; set; } = [];
    public List<LocationSummary> RecentLocations { get; set; } = [];
    public DashboardStats Stats { get; set; } = new();
    public UsageSummary? Usage { get; set; }

    // === Filter Properties ===
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
    public int TotalJobs { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalJobs / PageSize);

    // === Messages ===
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Site Evaluator Dashboard";
        ViewData["NoIndex"] = true;

        try
        {
            // Get user ID (in a real app, from claims)
            var userId = User.Identity?.Name ?? "anonymous";

            // Build filter
            var filter = new JobListFilter
            {
                Skip = (PageNumber - 1) * PageSize,
                Take = PageSize,
                SortBy = "CreatedDate",
                SortDescending = true
            };

            if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<JobStatus>(StatusFilter, out var status))
            {
                filter.Status = status;
            }

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                filter.CustomerName = SearchQuery;
            }

            // Get jobs
            var allJobs = await _jobService.GetUserJobsAsync(userId, filter);
            RecentJobs = allJobs.ToList();

            // Get total count for pagination
            var allJobsUnfiltered = await _jobService.GetUserJobsAsync(userId);
            TotalJobs = allJobsUnfiltered.Count();

            // Get recent locations
            var locations = await _locationService.GetLocationSummariesAsync(0, 5);
            RecentLocations = locations.ToList();

            // Calculate stats
            var allJobsList = allJobsUnfiltered.ToList();
            Stats = new DashboardStats
            {
                TotalJobs = allJobsList.Count,
                CompletedJobs = allJobsList.Count(j => j.Status == JobStatus.Complete),
                InProgressJobs = allJobsList.Count(j => j.Status == JobStatus.InProgress || j.Status == JobStatus.DataCollection),
                TotalReports = allJobsList.Sum(j => j.Reports.Count),
                UniqueLocations = allJobsList.Select(j => j.LocationId).Distinct().Count()
            };

            // Get usage/subscription info
            Usage = await GetUsageSummaryAsync(userId);

            // Check for messages in TempData
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
            _logger.LogError(ex, "Error loading dashboard");
            ErrorMessage = "Failed to load dashboard data.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteJobAsync(string jobId)
    {
        try
        {
            var result = await _jobService.DeleteJobAsync(jobId);
            if (result)
            {
                TempData["SuccessMessage"] = "Job cancelled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Job not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job {JobId}", jobId);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task<UsageSummary?> GetUsageSummaryAsync(string userId)
    {
        try
        {
            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            if (subscription != null)
            {
                return new UsageSummary
                {
                    Tier = subscription.Tier,
                    SearchesUsed = subscription.SearchesThisMonth,
                    SearchesLimit = SubscriptionTierConfig.GetSearchesPerMonth(subscription.Tier),
                    ReportsUsed = subscription.ReportsThisMonth,
                    ResetDate = subscription.UsageResetDate
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get usage summary for user {UserId}", userId);
        }
        return null;
    }
}

public class DashboardStats
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int InProgressJobs { get; set; }
    public int TotalReports { get; set; }
    public int UniqueLocations { get; set; }
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
    public int UsagePercent => SearchesLimit > 0 ? (int)((double)SearchesUsed / SearchesLimit * 100) : 0;
}
