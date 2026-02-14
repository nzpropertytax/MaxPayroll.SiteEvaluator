# Max Site Evaluator

<div align="center">

## ??? Site Evaluation Made Simple

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   A MaxPayroll.Website Platform Extension                       ?
?   All your site data in one place                               ?
?   From address to engineer's report in minutes, not days        ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## ?? Overview

Max Site Evaluator is a **module/extension** of the MaxPayroll.Website platform that aggregates site data from multiple New Zealand government and council sources to help engineers, developers, and planners perform site due diligence quickly and efficiently.

### Key Features

| Feature | Description |
|---------|-------------|
| **Address Search** | Enter any NZ address, get comprehensive site data |
| **Data Aggregation** | LINZ, NZGD, Council GIS, GNS, NIWA - all in one place |
| **Professional Reports** | Generate PS1-ready PDFs in seconds |
| **API Access** | Integrate with your existing workflows |
| **Platform Integration** | Uses existing platform auth and user system |

---

## ??? Architecture

This is a **Razor Class Library** that extends the main MaxPayroll.Website platform:

- Uses **existing platform authentication** (no separate user system)
- Stores data in **site's LiteDB database** via `ISiteDatabaseRepository`
- Adds **subscription tracking** as a feature layer
- Provides **API endpoints** that can be mounted on the main application

### Project Structure

```
MaxPayroll.SiteEvaluator/
??? Components/                    # Blazor Static SSR components
?   ??? Pages/                     # Page components
?   ??? MainLayout.razor           # Layout (uses platform layout)
?
??? Models/                        # Domain models
?   ??? SiteEvaluation.cs          # Main evaluation model
?   ??? ZoningData.cs              # Zoning & planning
?   ??? HazardData.cs              # Natural hazards
?   ??? GeotechnicalData.cs        # Geotech data
?   ??? InfrastructureData.cs      # Utilities
?   ??? LandData.cs                # LINZ title data
?   ??? ClimateData.cs             # NIWA climate data
?   ??? Subscription.cs            # Feature subscription tracking
?
??? Services/                      # Business logic
?   ??? Interfaces.cs              # Service interfaces
?   ??? SiteSearchService.cs       # Main search (uses ISiteDatabaseRepository)
?   ??? ReportService.cs           # PDF generation
?   ??? SubscriptionService.cs     # Feature billing
?   ??? Integration/               # External API clients
?
??? Endpoints/                     # REST API endpoints
?   ??? SiteEvaluatorEndpoints.cs
?
??? wwwroot/                       # Static assets
??? Program.cs                     # Service registration extensions
```

---

## ?? Integration with Platform

### 1. Add to Main Program.cs

```csharp
// In MaxPayroll.Website/Program.cs

using MaxPayroll.SiteEvaluator;

// Add services
builder.Services.AddSiteEvaluator(builder.Configuration);

// Map endpoints
app.MapSiteEvaluatorEndpoints();
```

### 2. Configuration (appsettings.json)

```json
{
  "SiteEvaluator": {
    "Linz": {
      "BaseUrl": "https://data.linz.govt.nz",
      "ApiKey": "your-api-key"
    },
    "Nzgd": {
      "BaseUrl": "https://www.nzgd.org.nz",
      "ApiKey": "your-api-key"
    }
  }
}
```

### 3. Enable Feature per Site

The Site Evaluator feature can be enabled/disabled per site via the platform's feature flags.

---

## ?? API Reference

All endpoints are prefixed with `/api/siteevaluator/`:

### Search Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/search/address` | Search by address |
| POST | `/search/title` | Search by title reference |
| POST | `/search/coordinates` | Search by lat/lon |

### Evaluation Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/evaluations/{id}` | Get evaluation |
| GET | `/evaluations` | List user evaluations |
| DELETE | `/evaluations/{id}` | Delete evaluation |
| POST | `/evaluations/{id}/refresh` | Refresh data |

### Report Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/reports/{id}/full` | Full PDF report |
| GET | `/reports/{id}/summary` | Summary PDF |
| GET | `/reports/{id}/geotech` | Geotech brief |

---

## ?? Data Sources

| Source | Data Provided | Integration Status |
|--------|--------------|-------------------|
| **LINZ** | Addresses, titles, boundaries | ? Ready |
| **NZGD** | Boreholes, CPTs, reports | ? Ready |
| **Christchurch CC** | Zoning, hazards, infrastructure | ? Ready |
| **Auckland Council** | Zoning, hazards, infrastructure | ?? Planned |
| **Wellington CC** | Zoning, hazards, infrastructure | ?? Planned |
| **GNS Science** | Seismic hazards, faults | ? Ready |
| **NIWA** | Rainfall, wind zones | ? Ready |

---

## ?? Subscription Tiers (Feature Add-On)

Site Evaluator subscriptions are tracked per-user as a platform feature:

| Tier | Price | Searches/Month | Features |
|------|-------|----------------|----------|
| Pay-per-search | $25/search | As needed | Basic reports |
| Starter | $99/month | 10 | Basic reports |
| Professional | $299/month | 50 | Full reports, API |
| Enterprise | $599/month | Unlimited | Team usage, priority support |

---

## ?? Related Documentation

- [Business Plan](../../MaxPayroll.Website.Platform/Docs/super-admin/portfolio-business-plans/Max-Site-Evaluator-Business-Plan.md)
- [Implementation Plan](../../MaxPayroll.Website.Platform/Docs/super-admin/portfolio-business-plans/Max-Site-Evaluator-Implementation-Plan.md)
- [Platform Database Architecture](../../MaxPayroll.Website/Docs/architecture/Database-Architecture.md)

---

## ?? License

Part of MaxPayroll.Website Platform - Proprietary

