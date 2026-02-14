using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaxPayroll.SiteEvaluator.Pages.SiteEvaluator.Admin;

/// <summary>
/// Admin page for managing external API credentials.
/// Credentials are stored in the database with encryption for sensitive fields.
/// </summary>
[Authorize(Roles = "Admin")]
public class CredentialsModel : PageModel
{
    private readonly IExternalCredentialService _credentialService;
    private readonly ILogger<CredentialsModel> _logger;

    public CredentialsModel(
        IExternalCredentialService credentialService,
        ILogger<CredentialsModel> logger)
    {
        _credentialService = credentialService;
        _logger = logger;
    }

    // === View Properties ===
    
    public List<ExternalApiCredential> Credentials { get; set; } = [];
    public ExternalApiCredential? SelectedCredential { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // === Bind Properties ===

    [BindProperty]
    public string? EditId { get; set; }

    [BindProperty]
    public CredentialInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? id = null)
    {
        ViewData["Title"] = "API Credentials";
        ViewData["NoIndex"] = true;

        // Check authorization
        if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Load all credentials
        Credentials = (await _credentialService.GetAllAsync()).ToList();

        // If editing a specific credential
        if (!string.IsNullOrEmpty(id))
        {
            SelectedCredential = await _credentialService.GetByIdAsync(id);
            if (SelectedCredential != null)
            {
                EditId = id;
                Input = CredentialInput.FromCredential(SelectedCredential);
            }
        }

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

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrEmpty(EditId))
        {
            TempData["ErrorMessage"] = "No credential selected.";
            return RedirectToPage();
        }

        try
        {
            var existing = await _credentialService.GetByIdAsync(EditId);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Credential not found.";
                return RedirectToPage();
            }

            // Update from input
            var credential = Input.ToCredential(existing);
            credential.Id = EditId;

            var success = await _credentialService.SaveAsync(credential, User.Identity?.Name);

            if (success)
            {
                TempData["SuccessMessage"] = $"? Credential '{credential.ServiceName}' saved successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to save credential.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving credential {Id}", EditId);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { id = EditId });
    }

    public async Task<IActionResult> OnPostTestAsync(string id)
    {
        try
        {
            var (success, message) = await _credentialService.TestCredentialAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing credential {Id}", id);
            TempData["ErrorMessage"] = $"Test failed: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleAsync(string id)
    {
        try
        {
            var credential = await _credentialService.GetByIdAsync(id);
            if (credential != null)
            {
                credential.IsEnabled = !credential.IsEnabled;
                await _credentialService.SaveAsync(credential, User.Identity?.Name);
                TempData["SuccessMessage"] = credential.IsEnabled
                    ? $"? {credential.ServiceName} enabled"
                    : $"?? {credential.ServiceName} disabled";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling credential {Id}", id);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSeedAsync()
    {
        try
        {
            await _credentialService.SeedDefaultCredentialsAsync();
            TempData["SuccessMessage"] = "? Default credentials seeded successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding credentials");
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }
}

/// <summary>
/// Input model for credential editing form.
/// </summary>
public class CredentialInput
{
    public string ServiceName { get; set; } = "";
    public string? Description { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AccessToken { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? WebhookSecret { get; set; }
    public string? Notes { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsTestMode { get; set; }

    public static CredentialInput FromCredential(ExternalApiCredential cred)
    {
        return new CredentialInput
        {
            ServiceName = cred.ServiceName,
            Description = cred.Description,
            BaseUrl = cred.BaseUrl,
            ApiKey = cred.ApiKey,
            ApiSecret = cred.ApiSecret,
            Username = cred.Username,
            Password = cred.Password,
            AccessToken = cred.AccessToken,
            ClientId = cred.ClientId,
            ClientSecret = cred.ClientSecret,
            WebhookSecret = cred.WebhookSecret,
            Notes = cred.Notes,
            IsEnabled = cred.IsEnabled,
            IsTestMode = cred.IsTestMode
        };
    }

    public ExternalApiCredential ToCredential(ExternalApiCredential existing)
    {
        // Start with existing to preserve fields we don't edit
        existing.ServiceName = ServiceName;
        existing.Description = Description;
        existing.BaseUrl = BaseUrl;
        existing.Notes = Notes;
        existing.IsEnabled = IsEnabled;
        existing.IsTestMode = IsTestMode;

        // Only update secrets if they were changed (not masked)
        if (!string.IsNullOrEmpty(ApiKey) && !ApiKey.Contains("****"))
            existing.ApiKey = ApiKey;
        if (!string.IsNullOrEmpty(ApiSecret) && !ApiSecret.Contains("****"))
            existing.ApiSecret = ApiSecret;
        if (!string.IsNullOrEmpty(Username))
            existing.Username = Username;
        if (!string.IsNullOrEmpty(Password) && !Password.Contains("****"))
            existing.Password = Password;
        if (!string.IsNullOrEmpty(AccessToken) && !AccessToken.Contains("****"))
            existing.AccessToken = AccessToken;
        if (!string.IsNullOrEmpty(ClientId))
            existing.ClientId = ClientId;
        if (!string.IsNullOrEmpty(ClientSecret) && !ClientSecret.Contains("****"))
            existing.ClientSecret = ClientSecret;
        if (!string.IsNullOrEmpty(WebhookSecret) && !WebhookSecret.Contains("****"))
            existing.WebhookSecret = WebhookSecret;

        return existing;
    }
}
