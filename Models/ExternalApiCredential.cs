using System.Text.Json.Serialization;

namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Stores external API credentials for third-party services.
/// Credentials are encrypted at rest in the database.
/// </summary>
public class ExternalApiCredential
{
    /// <summary>
    /// Unique identifier for the credential (e.g., "linz-address", "linz-landonline", "stripe").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the service (e.g., "LINZ Address API").
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Service provider/category (e.g., "LINZ", "GNS", "NIWA", "Stripe").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this API is used for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base URL for the API.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// API Key (encrypted at rest).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API Secret/Password (encrypted at rest).
    /// </summary>
    public string? ApiSecret { get; set; }

    /// <summary>
    /// Username for APIs that require username/password authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password (encrypted at rest).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Bearer/Access Token (encrypted at rest).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh Token for OAuth flows (encrypted at rest).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiry date for OAuth tokens.
    /// </summary>
    public DateTime? TokenExpiry { get; set; }

    /// <summary>
    /// Client ID for OAuth flows.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client Secret for OAuth flows (encrypted at rest).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Webhook secret for webhook signature verification (encrypted at rest).
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Additional custom fields as key-value pairs.
    /// Values may be encrypted based on field name conventions.
    /// </summary>
    public Dictionary<string, string>? CustomFields { get; set; }

    /// <summary>
    /// Whether this credential is currently active/enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether this is a test/sandbox credential vs production.
    /// </summary>
    public bool IsTestMode { get; set; }

    /// <summary>
    /// Environment tag (e.g., "production", "staging", "development").
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    /// Last time this credential was successfully used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Last time this credential was tested/validated.
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Result of last test (true = success, false = failed, null = not tested).
    /// </summary>
    public bool? LastTestResult { get; set; }

    /// <summary>
    /// Error message from last failed test.
    /// </summary>
    public string? LastTestError { get; set; }

    /// <summary>
    /// Notes about this credential.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// URL to the service's documentation.
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// URL to manage/renew this credential.
    /// </summary>
    public string? ManagementUrl { get; set; }

    /// <summary>
    /// When this credential expires (for subscription-based APIs).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Who created this credential.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this credential was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who last modified this credential.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// When this credential was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    // === Helper Properties ===

    /// <summary>
    /// Whether this credential has any authentication configured.
    /// </summary>
    [JsonIgnore]
    public bool HasAuthentication =>
        !string.IsNullOrEmpty(ApiKey) ||
        !string.IsNullOrEmpty(ApiSecret) ||
        !string.IsNullOrEmpty(AccessToken) ||
        !string.IsNullOrEmpty(Username) ||
        !string.IsNullOrEmpty(ClientId);

    /// <summary>
    /// Whether the token has expired (for OAuth tokens).
    /// </summary>
    [JsonIgnore]
    public bool IsTokenExpired =>
        TokenExpiry.HasValue && TokenExpiry.Value < DateTime.UtcNow;

    /// <summary>
    /// Whether the credential subscription has expired.
    /// </summary>
    [JsonIgnore]
    public bool IsSubscriptionExpired =>
        ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Summary status of the credential.
    /// </summary>
    [JsonIgnore]
    public CredentialStatus Status
    {
        get
        {
            if (!IsEnabled)
                return CredentialStatus.Disabled;
            if (IsSubscriptionExpired)
                return CredentialStatus.Expired;
            if (IsTokenExpired)
                return CredentialStatus.TokenExpired;
            if (!HasAuthentication)
                return CredentialStatus.NotConfigured;
            if (LastTestResult == false)
                return CredentialStatus.Failed;
            if (LastTestResult == true)
                return CredentialStatus.Active;
            return CredentialStatus.Unknown;
        }
    }
}

/// <summary>
/// Status of an external API credential.
/// </summary>
public enum CredentialStatus
{
    /// <summary>Credential is active and last test passed.</summary>
    Active,
    
    /// <summary>Credential exists but hasn't been tested.</summary>
    Unknown,
    
    /// <summary>Credential is disabled.</summary>
    Disabled,
    
    /// <summary>Credential subscription has expired.</summary>
    Expired,
    
    /// <summary>OAuth token has expired and needs refresh.</summary>
    TokenExpired,
    
    /// <summary>Last connection test failed.</summary>
    Failed,
    
    /// <summary>No authentication credentials configured.</summary>
    NotConfigured
}

/// <summary>
/// Predefined credential IDs for known services.
/// </summary>
public static class KnownCredentialIds
{
    // LINZ Services
    public const string LinzAddress = "linz-address";
    public const string LinzLandonline = "linz-landonline";
    public const string LinzWfs = "linz-wfs";
    
    // Geotechnical
    public const string Nzgd = "nzgd";
    
    // Hazard Data
    public const string GnsScience = "gns-science";
    public const string GeoNet = "geonet";
    
    // Climate
    public const string NiwaHirds = "niwa-hirds";
    public const string NiwaClimate = "niwa-climate";
    
    // Council GIS
    public const string ChristchurchCouncil = "council-christchurch";
    public const string AucklandCouncil = "council-auckland";
    public const string WellingtonCouncil = "council-wellington";
    
    // Billing
    public const string Stripe = "stripe";
    public const string StripeWebhook = "stripe-webhook";
}
