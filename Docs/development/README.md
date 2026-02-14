# Development Documentation ??

Technical documentation for developers working on Site Evaluator.

---

## Documentation in This Folder

| Document | Description |
|----------|-------------|
| [Job Architecture Guide](Job-Architecture-Guide.md) | **NEW** — Job-based data model and workflow |
| [Service Implementation Guide](Service-Implementation-Guide.md) | Adding new data services |
| [AI Learning Model Guide](AI-Learning-Model-Guide.md) | AI-powered recommendations and learning |

---

## Quick Overview

### Core Architecture

The Site Evaluator uses a **Job-based architecture**:

```
EvaluationJob ? PropertyLocation ? Cached Data
     ?
  JobReport (PDF)
```

- **Job** = One engagement/request (can bill separately)
- **Location** = Shared property data (cached)
- **Report** = Generated PDF for client delivery

### Project Structure

```
MaxPayroll.SiteEvaluator/
??? Configuration/           # Service registration
??? Docs/                    # Documentation
?   ??? API-Implementation-Guides/
?   ??? development/         # You are here
?   ??? user-guides/
??? Endpoints/               # Minimal API endpoints
??? Models/                  # Data models
?   ??? Wizard/             # Wizard state models
??? Pages/                   # Razor Pages
?   ??? SiteEvaluator/      # Wizard pages
??? Services/                # Business logic
?   ??? Integration/        # External API services
??? wwwroot/                 # Static assets
    ??? css/
    ??? js/
```

### Key Services

| Service | Purpose |
|---------|---------|
| `JobService` | **NEW** — Job management and workflow |
| `LocationService` | **NEW** — Property locations with caching |
| `SiteSearchService` | Legacy search orchestration |
| `LinzDataService` | LINZ address/property lookup |
| `CouncilGisService` | Council GIS data |
| `NzgdDataService` | Geotechnical database |
| `ReportService` | Report generation |

---

## Related Documentation

- [API Implementation Guides](../API-Implementation-Guides/README.md)
- [User Guides](../user-guides/README.md)
