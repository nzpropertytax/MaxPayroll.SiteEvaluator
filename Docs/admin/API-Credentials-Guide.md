# API Credentials Configuration Guide

**Location**: Admin ? Settings ? API Credentials  
**URL**: `/SiteEvaluator/Admin/Credentials`

---

## ?? Overview

External API credentials are now stored **in the database** with AES-256 encryption, rather than in `appsettings.json`. This provides:

- ? **Better security** - Credentials encrypted at rest
- ? **Easier management** - Edit via admin UI
- ? **Audit trail** - Track who modified credentials and when
- ? **Connection testing** - Verify credentials work before using

---

## ?? Credential Storage

### Database Location

Credentials are stored in: `Data/site-evaluator.db`  
Collection: `external_api_credentials`

### Encryption

| Field Type | Encryption |
|------------|------------|
| API Keys | ? AES-256 encrypted |
| Secrets | ? AES-256 encrypted |
| Passwords | ? AES-256 encrypted |
| Tokens | ? AES-256 encrypted |
| URLs | ? Plain text |
| Names | ? Plain text |

### Encryption Key

The encryption key is stored in `Data/.credential-key` (hidden file). 

For production, set the key via environment variable:
```bash
export SiteEvaluator__CredentialEncryptionKey="your-base64-encoded-32-byte-key"
```

---

## ?? Configured Services

### Required Services

| Service | ID | Free? | Purpose |
|---------|-----|-------|---------|
| **LINZ Address API** | `linz-address` | ? Yes | Address autocomplete & geocoding |

### Optional Services

| Service | ID | Cost | Purpose |
|---------|-----|------|---------|
| **LINZ Landonline** | `linz-landonline` | ~$500/yr | Property titles & ownership |
| **NZGD** | `nzgd` | Free | Geotechnical data (boreholes, CPTs) |
| **GNS Science** | `gns-science` | Free | Seismic hazard data |
| **NIWA HIRDS** | `niwa-hirds` | Free | Rainfall intensity data |
| **Stripe** | `stripe` | Per transaction | Payment processing |

### Council GIS Services (Free - No API Key Required)

| Service | ID | Coverage |
|---------|-----|----------|
| Christchurch City Council | `council-christchurch` | Canterbury region |
| Auckland Council | `council-auckland` | Auckland region |
| Wellington City Council | `council-wellington` | Wellington region |

---

## ??? Getting API Keys

### LINZ Address API (Required)

1. Go to [data.linz.govt.nz](https://data.linz.govt.nz/)
2. Create a free account
3. Navigate to **My Account** ? **API Keys**
4. Click **Generate Key**
5. Copy the key to the Credentials admin page

### LINZ Landonline (Optional - Paid)

1. Visit [LINZ Data Services](https://www.linz.govt.nz/products-services/data)
2. Apply for Landonline subscription (~$500/year)
3. Set up B2B API access
4. Configure OAuth credentials

### Stripe (For Billing)

1. Go to [dashboard.stripe.com](https://dashboard.stripe.com/)
2. Navigate to **Developers** ? **API Keys**
3. Copy the **Secret key** (starts with `sk_live_` or `sk_test_`)
4. Set up webhook endpoint and copy **Webhook secret**

---

## ?? Credential Status

| Status | Icon | Meaning |
|--------|------|---------|
| Active | ?? | Configured and last test passed |
| Unknown | ?? | Configured but not tested |
| Disabled | ? | Manually disabled |
| Failed | ?? | Last connection test failed |
| Not Configured | ? | No credentials entered |

---

## ?? Migration from appsettings.json

If you previously stored credentials in `appsettings.json`, they will still work. Database credentials take priority over config file settings.

### Priority Order

1. **Database credentials** (highest priority)
2. **Environment variables** 
3. **appsettings.json** (lowest priority)

### Migration Steps

1. Go to `/SiteEvaluator/Admin/Credentials`
2. Click **Seed Defaults** to create entries
3. Enter your existing API keys in the form
4. Click **Test Connection** to verify
5. Remove keys from `appsettings.json` once migrated

---

## ?? Testing Credentials

Each service has a **Test Connection** button that:

1. Attempts to connect to the API
2. Makes a simple test request
3. Records the result (success/failure)
4. Updates `LastTestedAt` timestamp

### Test Results

| Service | Test Method |
|---------|-------------|
| LINZ Address | Geocodes "1 Willis Street Wellington" |
| Stripe | Calls `/v1/balance` endpoint |
| NZGD | Queries boreholes near Christchurch |

---

## ?? Security Best Practices

1. **Use test keys during development** - Stripe provides `sk_test_` keys
2. **Rotate keys periodically** - Update credentials every 6-12 months
3. **Disable unused services** - Toggle off services you don't use
4. **Monitor usage** - Check `LastUsedAt` timestamps
5. **Backup encryption key** - Store `.credential-key` securely

---

## Related Documentation

- [LINZ Address API Guide](../API-Implementation-Guides/LINZ-Address-API.md)
- [LINZ Landonline API Guide](../API-Implementation-Guides/LINZ-Landonline-API.md)
- [Service Implementation Guide](Service-Implementation-Guide.md)
