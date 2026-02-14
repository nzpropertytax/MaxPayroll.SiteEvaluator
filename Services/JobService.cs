using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Models.Wizard;
using MaxPayroll.SiteEvaluator.Services.Integration;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Manages evaluation jobs.
/// </summary>
public class JobService : IJobService
{
    private readonly ISiteEvaluatorRepository _repository;
    private readonly ILocationService _locationService;
    private readonly IReportService _reportService;
    private readonly ILogger<JobService> _logger;

    // Data collection services
    private readonly IEnumerable<ICouncilDataService> _councilServices;
    private readonly IGnsDataService _gnsService;
    private readonly INiwaDataService _niwaService;
    private readonly INzgdDataService _nzgdService;

    public JobService(
        ISiteEvaluatorRepository repository,
        ILocationService locationService,
        IReportService reportService,
        IEnumerable<ICouncilDataService> councilServices,
        IGnsDataService gnsService,
        INiwaDataService niwaService,
        INzgdDataService nzgdService,
        ILogger<JobService> logger)
    {
        _repository = repository;
        _locationService = locationService;
        _reportService = reportService;
        _councilServices = councilServices;
        _gnsService = gnsService;
        _niwaService = niwaService;
        _nzgdService = nzgdService;
        _logger = logger;
    }

    public async Task<EvaluationJob> CreateJobAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new job for address: {Address}", request.Address);

        // Get or create the location
        PropertyLocation location;
        if (!string.IsNullOrEmpty(request.ExistingLocationId))
        {
            location = await _locationService.GetLocationAsync(request.ExistingLocationId, ct)
                ?? throw new ArgumentException($"Location not found: {request.ExistingLocationId}");
        }
        else if (!string.IsNullOrEmpty(request.Address))
        {
            location = await _locationService.GetOrCreateByAddressAsync(request.Address, ct);
        }
        else if (!string.IsNullOrEmpty(request.TitleReference))
        {
            location = await _locationService.GetOrCreateByTitleAsync(request.TitleReference, ct);
        }
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            location = await _locationService.GetOrCreateByCoordinatesAsync(
                request.Latitude.Value, request.Longitude.Value, ct);
        }
        else
        {
            throw new ArgumentException("Address, title reference, coordinates, or existing location ID required");
        }

        // Generate job reference
        var jobReference = await GetNextJobReferenceAsync(ct);

        // Create the job
        var job = new EvaluationJob
        {
            JobReference = jobReference,
            Title = request.Title ?? $"Evaluation - {location.GetShortAddress()}",
            LocationId = location.Id,
            Address = location.Address,
            CustomerName = request.CustomerName ?? string.Empty,
            CustomerReference = request.CustomerReference,
            CustomerEmail = request.CustomerEmail,
            CustomerCompany = request.CustomerCompany,
            Purpose = request.Purpose,
            Description = request.Description,
            IntendedUse = request.IntendedUse,
            IntendedUseDetails = request.IntendedUseDetails,
            IsNewDevelopment = request.IsNewDevelopment,
            ProposedHeight = request.ProposedHeight,
            ProposedCoverage = request.ProposedCoverage,
            ProposedUnits = request.ProposedUnits,
            ProposedGfa = request.ProposedGfa,
            IsBillable = request.IsBillable,
            InternalNotes = request.InternalNotes,
            Status = JobStatus.Created
        };

        await _repository.InsertAsync(job);
        _logger.LogInformation("Created job {JobReference} (ID: {JobId}) for location {LocationId}",
            job.JobReference, job.Id, location.Id);

        // Auto-start data collection if requested
        if (request.AutoStartDataCollection)
        {
            job = await StartDataCollectionAsync(job.Id, ct);
        }

        return job;
    }

    public async Task<EvaluationJob?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync<EvaluationJob>(jobId);
    }

    public async Task<EvaluationJob?> GetJobByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var jobs = await _repository.FindAsync<EvaluationJob>(j => j.JobReference == reference);
        return jobs.FirstOrDefault();
    }

    public async Task<EvaluationJob> UpdateJobAsync(string jobId, UpdateJobRequest request, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId)
            ?? throw new ArgumentException($"Job not found: {jobId}");

        // Update fields if provided
        if (request.Title != null) job.Title = request.Title;
        if (request.CustomerName != null) job.CustomerName = request.CustomerName;
        if (request.CustomerReference != null) job.CustomerReference = request.CustomerReference;
        if (request.CustomerEmail != null) job.CustomerEmail = request.CustomerEmail;
        if (request.CustomerCompany != null) job.CustomerCompany = request.CustomerCompany;
        if (request.Purpose.HasValue) job.Purpose = request.Purpose.Value;
        if (request.Description != null) job.Description = request.Description;
        if (request.IntendedUse.HasValue) job.IntendedUse = request.IntendedUse.Value;
        if (request.IntendedUseDetails != null) job.IntendedUseDetails = request.IntendedUseDetails;
        if (request.IsNewDevelopment.HasValue) job.IsNewDevelopment = request.IsNewDevelopment.Value;
        if (request.ProposedHeight.HasValue) job.ProposedHeight = request.ProposedHeight;
        if (request.ProposedCoverage.HasValue) job.ProposedCoverage = request.ProposedCoverage;
        if (request.ProposedUnits.HasValue) job.ProposedUnits = request.ProposedUnits;
        if (request.ProposedGfa.HasValue) job.ProposedGfa = request.ProposedGfa;
        if (request.IsBillable.HasValue) job.IsBillable = request.IsBillable.Value;
        if (request.InternalNotes != null) job.InternalNotes = request.InternalNotes;

        job.LastUpdated = DateTime.UtcNow;
        await _repository.UpdateAsync(job);

        return job;
    }

    public async Task<bool> DeleteJobAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId);
        if (job == null) return false;

        job.Status = JobStatus.Cancelled;
        job.LastUpdated = DateTime.UtcNow;
        await _repository.UpdateAsync(job);

        _logger.LogInformation("Job {JobReference} cancelled", job.JobReference);
        return true;
    }

    public async Task<IEnumerable<EvaluationJob>> GetUserJobsAsync(string userId, JobListFilter? filter = null, CancellationToken ct = default)
    {
        var jobs = await _repository.FindAsync<EvaluationJob>(j => j.CreatedByUserId == userId);

        if (filter != null)
        {
            if (filter.Status.HasValue)
                jobs = jobs.Where(j => j.Status == filter.Status.Value);
            if (filter.Purpose.HasValue)
                jobs = jobs.Where(j => j.Purpose == filter.Purpose.Value);
            if (filter.FromDate.HasValue)
                jobs = jobs.Where(j => j.CreatedDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                jobs = jobs.Where(j => j.CreatedDate <= filter.ToDate.Value);
            if (!string.IsNullOrEmpty(filter.CustomerName))
                jobs = jobs.Where(j => j.CustomerName.Contains(filter.CustomerName, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        jobs = (filter?.SortBy?.ToLowerInvariant()) switch
        {
            "jobreference" => filter.SortDescending ? jobs.OrderByDescending(j => j.JobReference) : jobs.OrderBy(j => j.JobReference),
            "customername" => filter.SortDescending ? jobs.OrderByDescending(j => j.CustomerName) : jobs.OrderBy(j => j.CustomerName),
            "status" => filter.SortDescending ? jobs.OrderByDescending(j => j.Status) : jobs.OrderBy(j => j.Status),
            _ => filter?.SortDescending == true ? jobs.OrderByDescending(j => j.CreatedDate) : jobs.OrderBy(j => j.CreatedDate)
        };

        // Pagination
        if (filter != null)
        {
            jobs = jobs.Skip(filter.Skip).Take(filter.Take);
        }

        return jobs.ToList();
    }

    public async Task<IEnumerable<EvaluationJob>> GetJobsForLocationAsync(string locationId, CancellationToken ct = default)
    {
        var jobs = await _repository.FindAsync<EvaluationJob>(j => j.LocationId == locationId);
        return jobs.OrderByDescending(j => j.CreatedDate).ToList();
    }

    public async Task<IEnumerable<EvaluationJob>> SearchJobsAsync(string query, CancellationToken ct = default)
    {
        var queryLower = query.ToLowerInvariant();
        var jobs = await _repository.FindAsync<EvaluationJob>(j =>
            j.JobReference.ToLower().Contains(queryLower) ||
            j.CustomerName.ToLower().Contains(queryLower) ||
            j.Address.ToLower().Contains(queryLower) ||
            (j.CustomerReference != null && j.CustomerReference.ToLower().Contains(queryLower)));

        return jobs.OrderByDescending(j => j.CreatedDate).Take(50).ToList();
    }

    public async Task<EvaluationJob> StartDataCollectionAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId)
            ?? throw new ArgumentException($"Job not found: {jobId}");

        var location = await _locationService.GetLocationAsync(job.LocationId, ct)
            ?? throw new InvalidOperationException($"Location not found for job: {jobId}");

        _logger.LogInformation("Starting data collection for job {JobReference}", job.JobReference);

        job.Status = JobStatus.DataCollection;
        job.StartedDate = DateTime.UtcNow;
        job.DataStatus.Location = DataSectionStatus.Complete;
        job.DataStatus.LocationUpdated = DateTime.UtcNow;

        // Refresh location data if stale
        await _locationService.RefreshLocationDataAsync(location.Id, null, ct);

        // Update data status based on cached data
        location = await _locationService.GetLocationAsync(job.LocationId, ct)!;

        job.DataStatus.Zoning = location.CachedZoning != null ? DataSectionStatus.Complete : DataSectionStatus.NotAvailable;
        job.DataStatus.ZoningUpdated = location.ZoningCachedAt;

        job.DataStatus.Hazards = location.CachedHazards != null ? DataSectionStatus.Complete : DataSectionStatus.NotAvailable;
        job.DataStatus.HazardsUpdated = location.HazardsCachedAt;

        job.DataStatus.Geotech = location.CachedGeotech != null ? DataSectionStatus.Complete : DataSectionStatus.NotAvailable;
        job.DataStatus.GeotechUpdated = location.GeotechCachedAt;

        job.DataStatus.Infrastructure = location.CachedInfrastructure != null ? DataSectionStatus.Complete : DataSectionStatus.NotAvailable;
        job.DataStatus.InfrastructureUpdated = location.InfrastructureCachedAt;

        job.DataStatus.Climate = location.CachedClimate != null ? DataSectionStatus.Complete : DataSectionStatus.NotAvailable;
        job.DataStatus.ClimateUpdated = location.ClimateCachedAt;

        job.DataStatus.Land = location.CachedLand != null ? DataSectionStatus.Complete : DataSectionStatus.NotAvailable;
        job.DataStatus.LandUpdated = location.LandCachedAt;

        // Calculate completeness
        job.CompletenessPercent = CalculateCompleteness(job.DataStatus);

        job.Status = JobStatus.Complete;
        job.CompletedDate = DateTime.UtcNow;
        job.LastUpdated = DateTime.UtcNow;

        await _repository.UpdateAsync(job);

        _logger.LogInformation("Data collection complete for job {JobReference}, {Completeness}% complete",
            job.JobReference, job.CompletenessPercent);

        return job;
    }

    public async Task<EvaluationJob> RefreshDataSectionsAsync(string jobId, IEnumerable<string> sections, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId)
            ?? throw new ArgumentException($"Job not found: {jobId}");

        await _locationService.RefreshLocationDataAsync(job.LocationId, sections, ct);

        // Re-run data collection to update status
        return await StartDataCollectionAsync(jobId, ct);
    }

    public async Task<JobReport> GenerateReportAsync(string jobId, ReportType type, ReportOptions options, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId)
            ?? throw new ArgumentException($"Job not found: {jobId}");

        var location = await _locationService.GetLocationAsync(job.LocationId, ct)
            ?? throw new InvalidOperationException($"Location not found for job: {jobId}");

        _logger.LogInformation("Generating {Type} report for job {JobReference}", type, job.JobReference);

        // Build evaluation from cached data
        var evaluation = BuildEvaluationFromLocation(location, job);

        // Generate report
        byte[] reportBytes = type switch
        {
            ReportType.SummaryReport => await _reportService.GenerateSummaryReportAsync(evaluation, ct),
            ReportType.GeotechBrief => await _reportService.GenerateGeotechBriefAsync(evaluation, ct),
            ReportType.DueDiligencePack => await _reportService.GenerateDueDiligencePackAsync(evaluation, ct),
            _ => await _reportService.GenerateFullReportAsync(evaluation, options, ct)
        };

        // Create report record
        var report = new JobReport
        {
            JobId = jobId,
            Type = type,
            Title = $"{type} - {job.JobReference}",
            FileName = $"{job.JobReference}_{type}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            FileSize = reportBytes.Length,
            Options = options
        };

        // Store report (in a real implementation, this would go to blob storage)
        report.StoragePath = $"reports/{job.Id}/{report.Id}.pdf";

        // Add to job
        job.Reports.Add(report);
        job.LastUpdated = DateTime.UtcNow;
        await _repository.UpdateAsync(job);

        // Store the actual file (simplified - just cache in memory for now)
        await _repository.StoreReportAsync(report.Id, reportBytes);

        _logger.LogInformation("Generated report {ReportId} for job {JobReference}, {Size} bytes",
            report.Id, job.JobReference, reportBytes.Length);

        return report;
    }

    public async Task<byte[]?> GetReportContentAsync(string jobId, string reportId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId);
        if (job == null) return null;

        var report = job.Reports.FirstOrDefault(r => r.Id == reportId);
        if (report == null) return null;

        // Update download tracking
        report.DownloadCount++;
        report.LastDownloaded = DateTime.UtcNow;
        await _repository.UpdateAsync(job);

        return await _repository.GetReportAsync(reportId);
    }

    public async Task<EvaluationJob> UpdateJobStatusAsync(string jobId, JobStatus status, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync<EvaluationJob>(jobId)
            ?? throw new ArgumentException($"Job not found: {jobId}");

        job.Status = status;
        job.LastUpdated = DateTime.UtcNow;

        if (status == JobStatus.Complete && !job.CompletedDate.HasValue)
        {
            job.CompletedDate = DateTime.UtcNow;
        }

        await _repository.UpdateAsync(job);
        return job;
    }

    public async Task<string> GetNextJobReferenceAsync(CancellationToken ct = default)
    {
        // Get the highest job reference for this year
        var year = DateTime.UtcNow.Year;
        var prefix = $"JOB-{year}-";

        var jobs = await _repository.FindAsync<EvaluationJob>(j => j.JobReference.StartsWith(prefix));
        var maxSequence = jobs
            .Select(j =>
            {
                var parts = j.JobReference.Split('-');
                return parts.Length == 3 && int.TryParse(parts[2], out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return EvaluationJob.GenerateJobReference(maxSequence + 1);
    }

    private static int CalculateCompleteness(JobDataStatus status)
    {
        var sections = new[]
        {
            status.Location,
            status.Zoning,
            status.Hazards,
            status.Geotech,
            status.Infrastructure,
            status.Climate,
            status.Land
        };

        var complete = sections.Count(s => s == DataSectionStatus.Complete);
        var partial = sections.Count(s => s == DataSectionStatus.Partial);

        return (int)((complete + partial * 0.5) / sections.Length * 100);
    }

    private static SiteEvaluation BuildEvaluationFromLocation(PropertyLocation location, EvaluationJob job)
    {
        return new SiteEvaluation
        {
            Id = job.Id,
            UserId = job.CreatedByUserId,
            CreatedDate = job.CreatedDate,
            LastUpdated = job.LastUpdated,
            Location = new SiteLocation
            {
                Address = location.Address,
                LegalDescription = location.LegalDescription ?? string.Empty,
                TitleReference = location.TitleReference,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Suburb = location.Suburb,
                City = location.City,
                TerritorialAuthority = location.TerritorialAuthority,
                RegionalCouncil = location.RegionalCouncil,
                Boundary = location.Boundary
            },
            Zoning = location.CachedZoning,
            Hazards = location.CachedHazards,
            Geotech = location.CachedGeotech,
            Infrastructure = location.CachedInfrastructure,
            Climate = location.CachedClimate,
            Land = location.CachedLand,
            Status = job.Status == JobStatus.Complete ? EvaluationStatus.Complete : EvaluationStatus.InProgress
        };
    }
}
