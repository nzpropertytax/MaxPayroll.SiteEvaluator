# Development Documentation ???

Technical documentation for developers working on Site Evaluator.

---

## Documentation in This Folder

| Document | Description |
|----------|-------------|
| [AI Learning Model Guide](AI-Learning-Model-Guide.md) | AI-powered recommendations and learning |
| [Service Implementation Guide](Service-Implementation-Guide.md) | Adding new data services |

---

## Quick Overview

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
| `SiteSearchService` | Main orchestration service |
| `LinzDataService` | LINZ address/property lookup |
| `CouncilGisService` | Council GIS data |
| `NzgdDataService` | Geotechnical database |
| `ReportService` | Report generation |

---

## Related Documentation

- [API Implementation Guides](../API-Implementation-Guides/README.md)
- [User Guides](../user-guides/README.md)
