using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Service for managing external API credentials.
/// Handles CRUD operations with encryption for sensitive fields.
/// </summary>
public interface IExternalCredentialService
{
    /// <summary>
    /// Gets all credentials (with sensitive fields masked for display).
    /// </summary>
    Task<IEnumerable<ExternalApiCredential>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a credential by ID (with sensitive fields masked for display).
    /// </summary>
    Task<ExternalApiCredential?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Gets a credential by ID with decrypted values (for actual API use).
    /// </summary>
    Task<ExternalApiCredential?> GetDecryptedAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Gets credentials by provider (e.g., "LINZ", "Stripe").
    /// </summary>
    Task<IEnumerable<ExternalApiCredential>> GetByProviderAsync(string provider, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a credential.
    /// Sensitive fields are automatically encrypted before storage.
    /// </summary>
    Task<bool> SaveAsync(ExternalApiCredential credential, string? modifiedBy = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a credential.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Tests a credential by attempting to connect to the API.
    /// </summary>
    Task<(bool Success, string Message)> TestCredentialAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Updates the last used timestamp for a credential.
    /// </summary>
    Task UpdateLastUsedAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Seeds default credential entries for known services.
    /// Does not overwrite existing credentials.
    /// </summary>
    Task SeedDefaultCredentialsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets API key for a service (convenience method).
    /// Returns null if credential doesn't exist or is disabled.
    /// </summary>
    Task<string?> GetApiKeyAsync(string credentialId, CancellationToken ct = default);

    /// <summary>
    /// Gets base URL for a service (convenience method).
    /// </summary>
    Task<string?> GetBaseUrlAsync(string credentialId, CancellationToken ct = default);
}
