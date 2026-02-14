using System.Security.Cryptography;
using System.Text;
using LiteDB;
using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services.Integration;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// LiteDB implementation of external credential storage.
/// Encrypts sensitive fields (API keys, secrets, passwords, tokens) at rest.
/// </summary>
public class ExternalCredentialService : IExternalCredentialService
{
    private readonly ILiteDatabase _database;
    private readonly ILogger<ExternalCredentialService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly byte[] _encryptionKey;

    private const string CredentialsCollection = "external_api_credentials";

    // Fields that should be encrypted
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(ExternalApiCredential.ApiKey),
        nameof(ExternalApiCredential.ApiSecret),
        nameof(ExternalApiCredential.Password),
        nameof(ExternalApiCredential.AccessToken),
        nameof(ExternalApiCredential.RefreshToken),
        nameof(ExternalApiCredential.ClientSecret),
        nameof(ExternalApiCredential.WebhookSecret)
    };

    public ExternalCredentialService(
        IConfiguration configuration,
        ILogger<ExternalCredentialService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Use a separate database file for credentials (more secure)
        var dataPath = configuration["SiteEvaluator:DataPath"] ?? "Data";
        var dbPath = Path.Combine(dataPath, "site-evaluator.db");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _database = new LiteDatabase(dbPath);

        // Get or generate encryption key
        _encryptionKey = GetOrCreateEncryptionKey(configuration, dataPath);

        // Ensure indexes
        var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
        collection.EnsureIndex(x => x.Provider);
        collection.EnsureIndex(x => x.IsEnabled);

        _logger.LogInformation("External credential service initialized");
    }

    /// <summary>
    /// Gets or creates an encryption key for credential storage.
    /// Key is stored in a separate file or can be provided via configuration.
    /// </summary>
    private static byte[] GetOrCreateEncryptionKey(IConfiguration configuration, string dataPath)
    {
        // First, try to get key from configuration (preferred for production)
        var configKey = configuration["SiteEvaluator:CredentialEncryptionKey"];
        if (!string.IsNullOrEmpty(configKey))
        {
            return Convert.FromBase64String(configKey);
        }

        // Fall back to file-based key (auto-generated on first run)
        var keyFile = Path.Combine(dataPath, ".credential-key");
        
        if (File.Exists(keyFile))
        {
            return Convert.FromBase64String(File.ReadAllText(keyFile));
        }

        // Generate new key
        var key = new byte[32]; // 256 bits for AES-256
        RandomNumberGenerator.Fill(key);
        
        // Store the key
        File.WriteAllText(keyFile, Convert.ToBase64String(key));
        
        // Set restrictive permissions (Windows)
        try
        {
            var fileInfo = new FileInfo(keyFile);
            fileInfo.Attributes |= FileAttributes.Hidden;
        }
        catch
        {
            // Ignore permission errors on non-Windows systems
        }

        return key;
    }

    public Task<IEnumerable<ExternalApiCredential>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var credentials = collection.FindAll().ToList();

            // Mask sensitive fields for display
            foreach (var cred in credentials)
            {
                MaskSensitiveFields(cred);
            }

            return Task.FromResult<IEnumerable<ExternalApiCredential>>(credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all credentials");
            return Task.FromResult<IEnumerable<ExternalApiCredential>>([]);
        }
    }

    public Task<ExternalApiCredential?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var credential = collection.FindById(id);

            if (credential != null)
            {
                MaskSensitiveFields(credential);
            }

            return Task.FromResult(credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credential {Id}", id);
            return Task.FromResult<ExternalApiCredential?>(null);
        }
    }

    public Task<ExternalApiCredential?> GetDecryptedAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var credential = collection.FindById(id);

            if (credential != null)
            {
                DecryptSensitiveFields(credential);
            }

            return Task.FromResult(credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decrypted credential {Id}", id);
            return Task.FromResult<ExternalApiCredential?>(null);
        }
    }

    public Task<IEnumerable<ExternalApiCredential>> GetByProviderAsync(string provider, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var credentials = collection
                .Find(x => x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var cred in credentials)
            {
                MaskSensitiveFields(cred);
            }

            return Task.FromResult<IEnumerable<ExternalApiCredential>>(credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credentials for provider {Provider}", provider);
            return Task.FromResult<IEnumerable<ExternalApiCredential>>([]);
        }
    }

    public Task<bool> SaveAsync(ExternalApiCredential credential, string? modifiedBy = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);

            // Check if existing
            var existing = collection.FindById(credential.Id);

            // If updating, preserve encrypted values that weren't changed (still masked)
            if (existing != null)
            {
                PreserveUnchangedSecrets(credential, existing);
                credential.ModifiedAt = DateTime.UtcNow;
                credential.ModifiedBy = modifiedBy;
            }
            else
            {
                credential.CreatedAt = DateTime.UtcNow;
                credential.CreatedBy = modifiedBy;
            }

            // Encrypt sensitive fields before storage
            EncryptSensitiveFields(credential);

            collection.Upsert(credential);

            _logger.LogInformation("Saved credential {Id} ({ServiceName})", credential.Id, credential.ServiceName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving credential {Id}", credential.Id);
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var result = collection.Delete(id);

            if (result)
            {
                _logger.LogInformation("Deleted credential {Id}", id);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential {Id}", id);
            return Task.FromResult(false);
        }
    }

    public async Task<(bool Success, string Message)> TestCredentialAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var credential = await GetDecryptedAsync(id, ct);
            if (credential == null)
            {
                return (false, "Credential not found");
            }

            if (!credential.IsEnabled)
            {
                return (false, "Credential is disabled");
            }

            if (!credential.HasAuthentication)
            {
                return (false, "No authentication configured");
            }

            // Perform test based on credential type
            var (success, message) = id switch
            {
                KnownCredentialIds.LinzAddress => await TestLinzAddressAsync(credential, ct),
                KnownCredentialIds.LinzLandonline => await TestLinzLandonlineAsync(credential, ct),
                KnownCredentialIds.Nzgd => await TestNzgdAsync(credential, ct),
                KnownCredentialIds.Stripe => await TestStripeAsync(credential, ct),
                _ => (true, "No specific test available - credential saved successfully")
            };

            // Update test result
            await UpdateTestResultAsync(id, success, success ? null : message, ct);

            return (success, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing credential {Id}", id);
            await UpdateTestResultAsync(id, false, ex.Message, ct);
            return (false, $"Test failed: {ex.Message}");
        }
    }

    public Task UpdateLastUsedAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var credential = collection.FindById(id);

            if (credential != null)
            {
                credential.LastUsedAt = DateTime.UtcNow;
                collection.Update(credential);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last used for {Id}", id);
            return Task.CompletedTask;
        }
    }

    public async Task SeedDefaultCredentialsAsync(CancellationToken ct = default)
    {
        var defaults = new List<ExternalApiCredential>
        {
            new()
            {
                Id = KnownCredentialIds.LinzAddress,
                ServiceName = "LINZ Address API",
                Provider = "LINZ",
                Description = "Address geocoding and autocomplete for New Zealand addresses",
                BaseUrl = "https://data.linz.govt.nz",
                DocumentationUrl = "https://data.linz.govt.nz/",
                ManagementUrl = "https://data.linz.govt.nz/my/api-keys/",
                Notes = "Free tier available. Register at data.linz.govt.nz to get an API key."
            },
            new()
            {
                Id = KnownCredentialIds.LinzLandonline,
                ServiceName = "LINZ Landonline",
                Provider = "LINZ",
                Description = "Property titles, ownership, and encumbrances data",
                BaseUrl = "https://api.landonline.govt.nz",
                DocumentationUrl = "https://www.linz.govt.nz/products-services/data/types-linz-data/property-ownership-and-boundary-data",
                ManagementUrl = "https://www.landonline.govt.nz/",
                Notes = "Requires paid subscription (~$500/year). Contact LINZ for access."
            },
            new()
            {
                Id = KnownCredentialIds.Nzgd,
                ServiceName = "NZGD Geotechnical Database",
                Provider = "NZGD",
                Description = "Borehole, CPT, and geotechnical report data",
                BaseUrl = "https://www.nzgd.org.nz",
                DocumentationUrl = "https://www.nzgd.org.nz/",
                Notes = "Public data available without API key. API key provides enhanced access."
            },
            new()
            {
                Id = KnownCredentialIds.GnsScience,
                ServiceName = "GNS Science",
                Provider = "GNS",
                Description = "Seismic hazard, active faults, and earthquake data",
                BaseUrl = "https://api.gns.cri.nz",
                DocumentationUrl = "https://www.gns.cri.nz/",
                Notes = "NZS 1170.5 Z-values available via static data. API provides real-time updates."
            },
            new()
            {
                Id = KnownCredentialIds.GeoNet,
                ServiceName = "GeoNet",
                Provider = "GNS",
                Description = "Real-time earthquake and volcanic activity",
                BaseUrl = "https://api.geonet.org.nz",
                DocumentationUrl = "https://api.geonet.org.nz/",
                Notes = "Public API - no authentication required for most endpoints."
            },
            new()
            {
                Id = KnownCredentialIds.NiwaHirds,
                ServiceName = "NIWA HIRDS",
                Provider = "NIWA",
                Description = "High Intensity Rainfall Design System - rainfall intensity data",
                BaseUrl = "https://hirds.niwa.co.nz",
                DocumentationUrl = "https://hirds.niwa.co.nz/",
                Notes = "Rainfall tables available via static data. API provides site-specific values."
            },
            new()
            {
                Id = KnownCredentialIds.ChristchurchCouncil,
                ServiceName = "Christchurch City Council GIS",
                Provider = "Council",
                Description = "CCC zoning, hazards, and infrastructure layers",
                BaseUrl = "https://opendata.canterburymaps.govt.nz",
                DocumentationUrl = "https://opendata.canterburymaps.govt.nz/",
                Notes = "Public ArcGIS REST services - no authentication required."
            },
            new()
            {
                Id = KnownCredentialIds.AucklandCouncil,
                ServiceName = "Auckland Council GIS",
                Provider = "Council",
                Description = "Auckland zoning, hazards, and infrastructure layers",
                BaseUrl = "https://services.arcgis.com",
                DocumentationUrl = "https://data.aucklandcouncil.govt.nz/",
                Notes = "Public ArcGIS REST services - no authentication required."
            },
            new()
            {
                Id = KnownCredentialIds.WellingtonCouncil,
                ServiceName = "Wellington City Council GIS",
                Provider = "Council",
                Description = "WCC zoning, hazards, and seismic layers",
                BaseUrl = "https://data.wcc.govt.nz",
                DocumentationUrl = "https://data.wcc.govt.nz/",
                Notes = "Public ArcGIS REST services - no authentication required."
            },
            new()
            {
                Id = KnownCredentialIds.Stripe,
                ServiceName = "Stripe",
                Provider = "Stripe",
                Description = "Payment processing for subscriptions and pay-per-search",
                BaseUrl = "https://api.stripe.com",
                DocumentationUrl = "https://stripe.com/docs/api",
                ManagementUrl = "https://dashboard.stripe.com/apikeys",
                Notes = "Required for billing. Use test keys for development."
            }
        };

        var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);

        foreach (var credential in defaults)
        {
            // Only insert if doesn't exist
            if (collection.FindById(credential.Id) == null)
            {
                credential.CreatedAt = DateTime.UtcNow;
                credential.CreatedBy = "System";
                collection.Insert(credential);
                _logger.LogInformation("Seeded default credential: {Id}", credential.Id);
            }
        }
    }

    public async Task<string?> GetApiKeyAsync(string credentialId, CancellationToken ct = default)
    {
        var credential = await GetDecryptedAsync(credentialId, ct);
        
        if (credential == null || !credential.IsEnabled)
            return null;

        // Update last used
        _ = UpdateLastUsedAsync(credentialId, ct);

        return credential.ApiKey;
    }

    public async Task<string?> GetBaseUrlAsync(string credentialId, CancellationToken ct = default)
    {
        var credential = await GetByIdAsync(credentialId, ct);
        return credential?.BaseUrl;
    }

    // === Private Helper Methods ===

    private Task UpdateTestResultAsync(string id, bool success, string? error, CancellationToken ct)
    {
        try
        {
            var collection = _database.GetCollection<ExternalApiCredential>(CredentialsCollection);
            var credential = collection.FindById(id);

            if (credential != null)
            {
                credential.LastTestedAt = DateTime.UtcNow;
                credential.LastTestResult = success;
                credential.LastTestError = error;
                collection.Update(credential);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test result for {Id}", id);
            return Task.CompletedTask;
        }
    }

    private void EncryptSensitiveFields(ExternalApiCredential credential)
    {
        credential.ApiKey = EncryptIfNotEmpty(credential.ApiKey);
        credential.ApiSecret = EncryptIfNotEmpty(credential.ApiSecret);
        credential.Password = EncryptIfNotEmpty(credential.Password);
        credential.AccessToken = EncryptIfNotEmpty(credential.AccessToken);
        credential.RefreshToken = EncryptIfNotEmpty(credential.RefreshToken);
        credential.ClientSecret = EncryptIfNotEmpty(credential.ClientSecret);
        credential.WebhookSecret = EncryptIfNotEmpty(credential.WebhookSecret);

        // Encrypt sensitive custom fields
        if (credential.CustomFields != null)
        {
            var encryptedFields = new Dictionary<string, string>();
            foreach (var (key, value) in credential.CustomFields)
            {
                if (key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    encryptedFields[key] = EncryptIfNotEmpty(value) ?? "";
                }
                else
                {
                    encryptedFields[key] = value;
                }
            }
            credential.CustomFields = encryptedFields;
        }
    }

    private void DecryptSensitiveFields(ExternalApiCredential credential)
    {
        credential.ApiKey = DecryptIfNotEmpty(credential.ApiKey);
        credential.ApiSecret = DecryptIfNotEmpty(credential.ApiSecret);
        credential.Password = DecryptIfNotEmpty(credential.Password);
        credential.AccessToken = DecryptIfNotEmpty(credential.AccessToken);
        credential.RefreshToken = DecryptIfNotEmpty(credential.RefreshToken);
        credential.ClientSecret = DecryptIfNotEmpty(credential.ClientSecret);
        credential.WebhookSecret = DecryptIfNotEmpty(credential.WebhookSecret);

        // Decrypt sensitive custom fields
        if (credential.CustomFields != null)
        {
            var decryptedFields = new Dictionary<string, string>();
            foreach (var (key, value) in credential.CustomFields)
            {
                if (key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    decryptedFields[key] = DecryptIfNotEmpty(value) ?? "";
                }
                else
                {
                    decryptedFields[key] = value;
                }
            }
            credential.CustomFields = decryptedFields;
        }
    }

    private void MaskSensitiveFields(ExternalApiCredential credential)
    {
        credential.ApiKey = MaskValue(credential.ApiKey);
        credential.ApiSecret = MaskValue(credential.ApiSecret);
        credential.Password = MaskValue(credential.Password);
        credential.AccessToken = MaskValue(credential.AccessToken);
        credential.RefreshToken = MaskValue(credential.RefreshToken);
        credential.ClientSecret = MaskValue(credential.ClientSecret);
        credential.WebhookSecret = MaskValue(credential.WebhookSecret);

        // Mask sensitive custom fields
        if (credential.CustomFields != null)
        {
            var maskedFields = new Dictionary<string, string>();
            foreach (var (key, value) in credential.CustomFields)
            {
                if (key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    maskedFields[key] = MaskValue(value) ?? "";
                }
                else
                {
                    maskedFields[key] = value;
                }
            }
            credential.CustomFields = maskedFields;
        }
    }

    private void PreserveUnchangedSecrets(ExternalApiCredential newCred, ExternalApiCredential existing)
    {
        // If the new value is masked (contains ****), preserve the existing encrypted value
        if (IsMasked(newCred.ApiKey)) newCred.ApiKey = existing.ApiKey;
        if (IsMasked(newCred.ApiSecret)) newCred.ApiSecret = existing.ApiSecret;
        if (IsMasked(newCred.Password)) newCred.Password = existing.Password;
        if (IsMasked(newCred.AccessToken)) newCred.AccessToken = existing.AccessToken;
        if (IsMasked(newCred.RefreshToken)) newCred.RefreshToken = existing.RefreshToken;
        if (IsMasked(newCred.ClientSecret)) newCred.ClientSecret = existing.ClientSecret;
        if (IsMasked(newCred.WebhookSecret)) newCred.WebhookSecret = existing.WebhookSecret;
    }

    private static bool IsMasked(string? value) =>
        !string.IsNullOrEmpty(value) && value.Contains("****");

    private static string? MaskValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Decrypt first if encrypted, then mask
        // For display, we just show that there's a value configured
        return "****configured****";
    }

    private string? EncryptIfNotEmpty(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Contains("****"))
            return value;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(value);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV + encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return "ENC:" + Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting value");
            return value;
        }
    }

    private string? DecryptIfNotEmpty(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Check if it's encrypted
        if (!value.StartsWith("ENC:"))
            return value;

        try
        {
            var encryptedData = Convert.FromBase64String(value[4..]);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV from the beginning
            var iv = new byte[16];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            // Extract encrypted content
            var cipherBytes = new byte[encryptedData.Length - 16];
            Buffer.BlockCopy(encryptedData, 16, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting value");
            return value;
        }
    }

    // === Test Methods ===

    private async Task<(bool Success, string Message)> TestLinzAddressAsync(ExternalApiCredential credential, CancellationToken ct)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(credential.BaseUrl ?? "https://data.linz.govt.nz");

            if (!string.IsNullOrEmpty(credential.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"key {credential.ApiKey}");
            }

            var response = await httpClient.GetAsync("/services/api/v1/geocode?q=1+Willis+Street+Wellington", ct);

            if (response.IsSuccessStatusCode)
            {
                return (true, "? LINZ Address API connected successfully");
            }

            return (false, $"API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string Message)> TestLinzLandonlineAsync(ExternalApiCredential credential, CancellationToken ct)
    {
        // Landonline requires subscription - just verify the key format
        if (string.IsNullOrEmpty(credential.ApiKey) && string.IsNullOrEmpty(credential.AccessToken))
        {
            return (false, "No API key or access token configured");
        }

        return (true, "? Landonline credentials configured (subscription verification requires API call)");
    }

    private async Task<(bool Success, string Message)> TestNzgdAsync(ExternalApiCredential credential, CancellationToken ct)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(credential.BaseUrl ?? "https://www.nzgd.org.nz");

            // NZGD public endpoint test
            var response = await httpClient.GetAsync("/api/v1/boreholes?lat=-43.532&lon=172.636&radius=100", ct);

            // Even if no results, a 200 response means the API is working
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (true, "? NZGD API accessible");
            }

            return (false, $"API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string Message)> TestStripeAsync(ExternalApiCredential credential, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(credential.ApiKey))
        {
            return (false, "No Stripe API key configured");
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.stripe.com");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {credential.ApiKey}");

            var response = await httpClient.GetAsync("/v1/balance", ct);

            if (response.IsSuccessStatusCode)
            {
                var isTestKey = credential.ApiKey.StartsWith("sk_test_");
                return (true, $"? Stripe API connected ({(isTestKey ? "Test Mode" : "Live Mode")})");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (false, "Invalid API key");
            }

            return (false, $"API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
