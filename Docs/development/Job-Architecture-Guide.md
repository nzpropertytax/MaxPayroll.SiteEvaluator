# Job-Based Architecture ??

<div align="center">

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   ?? JOBS: THE HEART OF SITE EVALUATION                          ?
?                                                                  ?
?   Each evaluation request = One Job                              ?
?   Multiple jobs can exist for the same property                  ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## ?? Overview

The Site Evaluator uses a **Job-based architecture** where each evaluation request creates a separate Job record. This enables:

| Benefit | Description |
|---------|-------------|
| **Multiple Reports** | Same property, different customers |
| **Audit Trail** | Track who requested what and when |
| **Billing** | Invoice each job separately |
| **Re-running** | Refresh data without affecting other jobs |

---

## ??? Data Model

### Job ? Location ? Data

```
???????????????????
?   EvaluationJob ? ? Customer, purpose, billing
?   (JOB-2025-001)?
???????????????????
         ?
         ? references
         ?
???????????????????
? PropertyLocation? ? Address, coordinates, boundaries
?   (shared)      ?
???????????????????
         ?
         ? caches
         ?
???????????????????
?   Data Sections ? ? Zoning, Hazards, Geotech, etc.
?   (cached)      ?
???????????????????
```

### Multiple Jobs, Same Location

```
Job JOB-2025-001 (Customer: Smith & Co, Purpose: Purchase)
Job JOB-2025-002 (Customer: Jones Ltd, Purpose: Development)
Job JOB-2025-003 (Customer: Internal, Purpose: Due Diligence)
         ?
         ????????? All reference ????????? PropertyLocation (123 Main St)
                                                   ?
                                                   ??? Cached data (shared)
```

---

## ?? Core Models

### EvaluationJob

| Field | Type | Description |
|-------|------|-------------|
| `Id` | string | Unique ID (`job_abc123`) |
| `JobReference` | string | Human-readable (`JOB-2025-00001`) |
| `Title` | string | Job title/description |
| `LocationId` | string | Reference to PropertyLocation |
| `Address` | string | Denormalized for display |
| **Customer Info** | | |
| `CustomerName` | string | Client name |
| `CustomerReference` | string? | Client's PO/file reference |
| `CustomerEmail` | string? | For report delivery |
| `CustomerCompany` | string? | Organization |
| **Context** | | |
| `Purpose` | JobPurpose | Purchase, Development, etc. |
| `IntendedUse` | PropertyUseCategory | Residential, Commercial, etc. |
| `IsNewDevelopment` | bool | New build or existing? |
| **Development Details** | | |
| `ProposedHeight` | double? | Meters |
| `ProposedCoverage` | double? | Percentage |
| `ProposedUnits` | int? | Number of units |
| `ProposedGfa` | double? | Gross floor area (m²) |
| **Status** | | |
| `Status` | JobStatus | Created, InProgress, Complete |
| `DataStatus` | JobDataStatus | Per-section completion |
| `CompletenessPercent` | int | 0-100 |
| **Billing** | | |
| `IsBillable` | bool | Is this job billable? |
| `BillingStatus` | BillingStatus | NotBilled, Invoiced, Paid |
| `InvoiceReference` | string? | Invoice number |
| **Reports** | | |
| `Reports` | List<JobReport> | Generated reports |

### PropertyLocation

Shared across jobs for the same property:

| Field | Type | Description |
|-------|------|-------------|
| `Id` | string | Unique ID |
| `Address` | string | Full formatted address |
| `TitleReference` | string? | LINZ title reference |
| `LegalDescription` | string? | Legal description |
| `Latitude`, `Longitude` | double | Coordinates |
| `Boundary` | List<Coordinate>? | Property boundary |
| `TerritorialAuthority` | string? | e.g., "Christchurch City Council" |
| **Cached Data** | | |
| `CachedZoning` | ZoningData? | Last retrieved zoning |
| `CachedHazards` | HazardData? | Last retrieved hazards |
| `CachedGeotech` | GeotechnicalData? | Last retrieved geotech |
| ... | | Other cached sections |

---

## ?? Workflow

### 1. Create Job

```csharp
var request = new CreateJobRequest
{
    Address = "123 Main Street, Christchurch",
    CustomerName = "Smith Engineering Ltd",
    CustomerReference = "PO-12345",
    Purpose = JobPurpose.Development,
    IntendedUse = PropertyUseCategory.Residential,
    ProposedUnits = 6,
    AutoStartDataCollection = true
};

var job = await _jobService.CreateJobAsync(request);
// job.JobReference = "JOB-2025-00042"
```

### 2. Data Collection

```csharp
// Automatic if AutoStartDataCollection = true
// Or manual:
await _jobService.StartDataCollectionAsync(job.Id);
```

### 3. Generate Reports

```csharp
var report = await _jobService.GenerateReportAsync(
    job.Id,
    ReportType.FullReport,
    new ReportOptions
    {
        CompanyName = "Acme Engineering",
        PreparedBy = "John Smith, CPEng",
        PreparedFor = "Smith Engineering Ltd"
    });
```

### 4. Download Report

```csharp
var content = await _jobService.GetReportContentAsync(job.Id, report.Id);
// Returns PDF bytes
```

---

## ?? Job Status Flow

```
Created ? InProgress ? DataCollection ? Review ? Complete
                 ?
            Cancelled (can happen from any state)
                 ?
              OnHold (can happen from any state)
```

| Status | Description |
|--------|-------------|
| `Created` | Job created, no work started |
| `InProgress` | Work in progress |
| `DataCollection` | API data collection running |
| `Review` | Data collected, pending review |
| `Complete` | All done, reports available |
| `Cancelled` | Job cancelled |
| `OnHold` | Temporarily paused |

---

## ?? Billing Integration

Each job can be billed separately:

| BillingStatus | Description |
|---------------|-------------|
| `NotBilled` | Job created, not yet billed |
| `Pending` | Ready for invoicing |
| `Invoiced` | Invoice sent |
| `Paid` | Payment received |
| `Waived` | Fee waived |
| `Disputed` | Billing dispute |

```csharp
// Mark job as invoiced
job.BillingStatus = BillingStatus.Invoiced;
job.InvoiceReference = "INV-2025-00123";
await _jobService.UpdateJobAsync(job.Id, updateRequest);
```

---

## ??? Report Types Per Job

Each job can have multiple reports:

| Report Type | Content | Use Case |
|-------------|---------|----------|
| `FullReport` | All sections | Client deliverable |
| `SummaryReport` | Key findings | Quick overview |
| `GeotechBrief` | Geotech focus | Investigation planning |
| `DueDiligencePack` | Full + appendices | Legal due diligence |
| `Custom` | Selected sections | Custom requests |

---

## ?? Querying Jobs

### By User

```csharp
var myJobs = await _jobService.GetUserJobsAsync(userId, new JobListFilter
{
    Status = JobStatus.Complete,
    FromDate = DateTime.UtcNow.AddMonths(-3)
});
```

### By Location

```csharp
var locationJobs = await _jobService.GetJobsForLocationAsync(locationId);
```

### Search

```csharp
var results = await _jobService.SearchJobsAsync("Smith Engineering");
// Searches job reference, customer name, address
```

---

## ?? Data Caching Strategy

Location data is cached to avoid redundant API calls:

| Section | Cache Duration | When to Refresh |
|---------|----------------|-----------------|
| Zoning | 24 hours | Council updates |
| Hazards | 24 hours | Mapping updates |
| Geotech | 24 hours | New investigations |
| Infrastructure | 24 hours | Network changes |
| Climate | 7 days | Rarely changes |
| Land | 24 hours | Title transactions |

```csharp
// Check if cache is stale
if (location.IsCacheStale("zoning", maxAgeHours: 24))
{
    await _locationService.RefreshLocationDataAsync(location.Id, ["zoning"]);
}
```

---

## ?? Related Documentation

- [Sample Engineering Report](../user-guides/Sample-Engineering-Report.md)
- [Service Implementation Guide](Service-Implementation-Guide.md)
- [AI Learning Model Guide](AI-Learning-Model-Guide.md)
