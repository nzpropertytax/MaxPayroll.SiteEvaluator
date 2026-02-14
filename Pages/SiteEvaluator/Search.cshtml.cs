using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaxPayroll.SiteEvaluator.Pages.SiteEvaluator;

public class SearchModel : PageModel
{
    private readonly ISiteSearchService _searchService;

    public SearchModel(ISiteSearchService searchService)
    {
        _searchService = searchService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Address { get; set; }

    public SiteEvaluation? Evaluation { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            return Page();
        }

        try
        {
            IsLoading = false;
            Evaluation = await _searchService.SearchByAddressAsync(Address, ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error searching for site: {ex.Message}";
        }

        return Page();
    }
}
