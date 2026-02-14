# Site Evaluator Documentation ??

<div align="center">

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   ??? PROPERTY DUE DILIGENCE FOR NEW ZEALAND                      ?
?                                                                  ?
?   Comprehensive site evaluation for engineers and developers     ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## ?? Overview

The **Site Evaluator** is a comprehensive property due diligence tool that aggregates data from authoritative New Zealand sources to produce professional engineering reports.

### Key Features

| Feature | Description |
|---------|-------------|
| **8-Step Wizard** | Guided evaluation workflow |
| **Multi-Source Data** | LINZ, Council GIS, NZGD, NIWA integration |
| **Professional Reports** | PDF reports for client deliverables |
| **AI Recommendations** | Learning model for improved suggestions |
| **Historical Data** | Track and reuse previous evaluations |

---

## ?? Documentation Index

### User Guides

For engineers and users of the Site Evaluator:

| Document | Description |
|----------|-------------|
| [Site Evaluation Wizard Guide](user-guides/Site-Evaluation-Wizard-Guide.md) | Complete guide to the 8-step wizard |
| [Engineering Report Guide](user-guides/Engineering-Report-Guide.md) | Using reports for client deliverables |
| [Data Sources Reference](user-guides/Data-Sources-Reference.md) | Understanding data sources and accuracy |

### API Implementation Guides

Technical documentation for API integrations:

| Document | Description |
|----------|-------------|
| [API Implementation Overview](API-Implementation-Guides/README.md) | Overview of all API integrations |
| [LINZ Address API](API-Implementation-Guides/LINZ-Address-API.md) | Address geocoding |
| [LINZ Landonline API](API-Implementation-Guides/LINZ-Landonline-API.md) | Title and survey data |
| [Council GIS APIs](API-Implementation-Guides/Council-GIS-APIs.md) | Council zoning and hazard data |
| [GNS Science API](API-Implementation-Guides/GNS-Science-API.md) | Seismic and fault data |
| [NZGD API](API-Implementation-Guides/NZGD-API.md) | Geotechnical database |
| [NIWA API](API-Implementation-Guides/NIWA-API.md) | Climate data |

### Development Guides

For developers extending the Site Evaluator:

| Document | Description |
|----------|-------------|
| [Development Overview](development/README.md) | Project structure and services |
| [AI Learning Model Guide](development/AI-Learning-Model-Guide.md) | AI-powered recommendations |

---

## ?? Quick Start

### Starting a New Evaluation

1. Navigate to **Site Evaluator** ? **New Evaluation**
2. Enter the property address
3. Follow the 8-step wizard:
   - **Step 1:** Address Entry
   - **Step 2:** Property Match
   - **Step 3:** Zoning Review
   - **Step 4:** Hazards Assessment
   - **Step 5:** Geotechnical Data
   - **Step 6:** Infrastructure
   - **Step 7:** Climate Data
   - **Step 8:** Summary & Reports
4. Download reports for client delivery

### Typical Use Cases

| User | Use Case |
|------|----------|
| Civil Engineers | Pre-purchase assessment, subdivision feasibility |
| Geotechnical Engineers | Site investigation scoping |
| Property Developers | Development feasibility |
| Valuers | Risk assessment |

---

## ??? Architecture

### Data Flow

```
???????????????????????????????????????????????????????????????????
?                    SITE EVALUATOR                                ?
?                                                                  ?
?  ????????????  ????????????  ????????????  ????????????        ?
?  ? LINZ     ?  ? Council  ?  ? GNS/NZGD ?  ? NIWA     ?        ?
?  ? APIs     ?  ? GIS      ?  ? APIs     ?  ? APIs     ?        ?
?  ????????????  ????????????  ????????????  ????????????        ?
?       ?             ?             ?             ?               ?
?       ???????????????????????????????????????????               ?
?                            ?                                     ?
?                     ???????????????                             ?
?                     ? Evaluation  ?                             ?
?                     ? Engine      ?                             ?
?                     ???????????????                             ?
?                            ?                                     ?
?                     ???????????????                             ?
?                     ? Report      ?                             ?
?                     ? Generation  ?                             ?
?                     ???????????????                             ?
???????????????????????????????????????????????????????????????????
```

### Project Structure

```
MaxPayroll.SiteEvaluator/
??? Configuration/           # Service registration
??? Docs/                    # Documentation (you are here)
?   ??? API-Implementation-Guides/
?   ??? development/
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

---

## ?? AI Learning Model

The Site Evaluator includes an AI learning model that improves over time:

| Feature | Description |
|---------|-------------|
| **Similar Sites** | Find comparable properties with known outcomes |
| **Recommendations** | AI-suggested investigations based on patterns |
| **Gap Detection** | Identify missing data automatically |
| **Narratives** | AI-generated report summaries |

See [AI Learning Model Guide](development/AI-Learning-Model-Guide.md) for details.

---

## ?? Data Sources

| Source | Data Type | Update Frequency |
|--------|-----------|------------------|
| LINZ | Addresses, titles | Weekly |
| Council GIS | Zoning, hazards | Monthly |
| NZGD | Geotechnical | Ongoing |
| GNS Science | Seismic, faults | As updated |
| NIWA | Climate | Historical |

See [Data Sources Reference](user-guides/Data-Sources-Reference.md) for details.

---

## ?? Support

| Issue | Action |
|-------|--------|
| Address not found | Try different format, use coordinates |
| Data missing | Use refresh button, check coverage |
| Report error | Contact support with evaluation ID |

---

<div align="center">

**??? Site Evaluator — Professional Due Diligence Made Simple**

*Built for New Zealand property professionals*

</div>
