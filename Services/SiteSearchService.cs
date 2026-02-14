using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services.Integration;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Main site search service implementation.
/// Uses self-contained ISiteEvaluatorRepository for storage.
/// </summary>
public class SiteSearchService : ISiteSearchService
{
    private readonly ISiteEvaluatorRepository _repository;
    private readonly ILinzDataService _linzService;
    private readonly INzgdDataService _nzgdService;
    private readonly IEnumerable<ICouncilDataService> _councilServices;
    private readonly IGnsDataService _gnsService;
    private readonly INiwaDataService _niwaService;
    private readonly ILogger<SiteSearchService> _logger;

    public SiteSearchService(
        ISiteEvaluatorRepository repository,
        ILinzDataService linzService,
        INzgdDataService nzgdService,
        IEnumerable<ICouncilDataService> councilServices,
        IGnsDataService gnsService,
        INiwaDataService niwaService,
        ILogger<SiteSearchService> logger)
    {
        _repository = repository;
        _linzService = linzService;
        _nzgdService = nzgdService;
        _councilServices = councilServices;
        _gnsService = gnsService;
        _niwaService = niwaService;
        _logger = logger;
    }

    public async Task<SiteEvaluation> SearchByAddressAsync(string address, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting site search for address: {Address}", address);
        
        // Step 1: Geocode the address using LINZ
        var location = await _linzService.LookupAddressAsync(address, ct);
        
        if (location == null)
        {
            _logger.LogWarning("Address not found: {Address}", address);
            return new SiteEvaluation
            {
                Status = EvaluationStatus.Error,
                Warnings = [$"Address not found: {address}"]
            };
        }

        // Step 2: Create evaluation and populate data
        var evaluation = new SiteEvaluation
        {
            Location = location,
            Status = EvaluationStatus.InProgress
        };

        // Step 3: Gather data from all sources in parallel
        await PopulateAllDataAsync(evaluation, ct);

        // Step 4: Calculate completeness
        evaluation.Completeness = CalculateCompleteness(evaluation);
        evaluation.Status = DetermineStatus(evaluation);

        // Step 5: Save to database
        await _repository.InsertAsync(evaluation);

        _logger.LogInformation("Site search completed for {Address}, Id: {Id}", address, evaluation.Id);
        
        return evaluation;
    }

    public async Task<SiteEvaluation> SearchByTitleAsync(string titleReference, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting site search for title: {Title}", titleReference);
        
        // Get land data from LINZ by title reference
        var landData = await _linzService.GetTitleDataAsync(titleReference, ct);
        
        if (landData == null)
        {
            return new SiteEvaluation
            {
                Status = EvaluationStatus.Error,
                Warnings = [$"Title not found: {titleReference}"]
            };
        }

        // Create evaluation with land data
        var evaluation = new SiteEvaluation
        {
            Location = new SiteLocation
            {
                TitleReference = titleReference,
                LegalDescription = landData.LegalDescription ?? string.Empty
            },
            Land = landData,
            Status = EvaluationStatus.InProgress
        };

        // For title search, we need coordinates to proceed
        if (evaluation.Location.Latitude == 0 || evaluation.Location.Longitude == 0)
        {
            evaluation.DataGaps.Add(new DataGap
            {
                Section = "Location",
                Field = "Coordinates",
                Reason = "Could not determine site coordinates from title reference",
                Severity = GapSeverity.Critical
            });
            evaluation.Status = EvaluationStatus.RequiresManualData;
            return evaluation;
        }

        await PopulateAllDataAsync(evaluation, ct);
        evaluation.Completeness = CalculateCompleteness(evaluation);
        evaluation.Status = DetermineStatus(evaluation);

        await _repository.InsertAsync(evaluation);

        return evaluation;
    }

    public async Task<SiteEvaluation> SearchByCoordinatesAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting site search for coordinates: {Lat}, {Lon}", latitude, longitude);
        
        var evaluation = new SiteEvaluation
        {
            Location = new SiteLocation
            {
                Latitude = latitude,
                Longitude = longitude
            },
            Status = EvaluationStatus.InProgress
        };

        await PopulateAllDataAsync(evaluation, ct);
        evaluation.Completeness = CalculateCompleteness(evaluation);
        evaluation.Status = DetermineStatus(evaluation);

        await _repository.InsertAsync(evaluation);

        return evaluation;
    }

    public async Task<SiteEvaluation> RefreshDataAsync(string evaluationId, IEnumerable<string> sections, CancellationToken ct = default)
    {
        var evaluation = await _repository.GetByIdAsync<SiteEvaluation>(evaluationId);
        
        if (evaluation == null)
        {
            throw new ArgumentException($"Evaluation not found: {evaluationId}");
        }

        foreach (var section in sections)
        {
            switch (section.ToLowerInvariant())
            {
                case "zoning":
                    evaluation.Zoning = await GetZoningDataAsync(evaluation.Location, ct);
                    break;
                case "hazards":
                    evaluation.Hazards = await GetHazardDataAsync(evaluation.Location, ct);
                    break;
                case "geotech":
                    evaluation.Geotech = await GetGeotechDataAsync(evaluation.Location, ct);
                    break;
                case "infrastructure":
                    evaluation.Infrastructure = await GetInfrastructureDataAsync(evaluation.Location, ct);
                    break;
                case "climate":
                    evaluation.Climate = await GetClimateDataAsync(evaluation.Location, ct);
                    break;
            }
        }

        evaluation.LastUpdated = DateTime.UtcNow;
        evaluation.Completeness = CalculateCompleteness(evaluation);
        evaluation.Status = DetermineStatus(evaluation);

        await _repository.UpdateAsync(evaluation);

        return evaluation;
    }

    public async Task<SiteEvaluation?> GetEvaluationAsync(string evaluationId, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync<SiteEvaluation>(evaluationId);
    }

    public async Task<IEnumerable<SiteEvaluation>> GetUserEvaluationsAsync(string userId, CancellationToken ct = default)
    {
        var all = await _repository.FindAsync<SiteEvaluation>(_ => true);
        return all.Where(e => e.UserId == userId).OrderByDescending(e => e.CreatedDate);
    }

    public async Task<bool> DeleteEvaluationAsync(string evaluationId, CancellationToken ct = default)
    {
        return await _repository.DeleteAsync(evaluationId);
    }

    // === Private helper methods ===

    private async Task PopulateAllDataAsync(SiteEvaluation evaluation, CancellationToken ct)
    {
        var location = evaluation.Location;

        // Run data gathering in parallel for better performance
        var zoningTask = GetZoningDataAsync(location, ct);
        var hazardTask = GetHazardDataAsync(location, ct);
        var geotechTask = GetGeotechDataAsync(location, ct);
        var infrastructureTask = GetInfrastructureDataAsync(location, ct);
        var climateTask = GetClimateDataAsync(location, ct);

        await Task.WhenAll(zoningTask, hazardTask, geotechTask, infrastructureTask, climateTask);

        evaluation.Zoning = await zoningTask;
        evaluation.Hazards = await hazardTask;
        evaluation.Geotech = await geotechTask;
        evaluation.Infrastructure = await infrastructureTask;
        evaluation.Climate = await climateTask;
    }

    private async Task<ZoningData?> GetZoningDataAsync(SiteLocation location, CancellationToken ct)
    {
        var council = _councilServices.FirstOrDefault(c => c.SupportsRegion(location.Latitude, location.Longitude));
        
        if (council == null)
        {
            _logger.LogWarning("No council service available for location: {Lat}, {Lon}", location.Latitude, location.Longitude);
            return null;
        }

        return await council.GetZoningDataAsync(location.Latitude, location.Longitude, ct);
    }

    private async Task<HazardData?> GetHazardDataAsync(SiteLocation location, CancellationToken ct)
    {
        var council = _councilServices.FirstOrDefault(c => c.SupportsRegion(location.Latitude, location.Longitude));
        
        if (council == null)
            return null;

        var hazardData = await council.GetHazardDataAsync(location.Latitude, location.Longitude, ct);
        
        // Supplement with GNS seismic data
        if (hazardData != null)
        {
            hazardData.Seismic = await _gnsService.GetSeismicHazardAsync(location.Latitude, location.Longitude, ct);
        }

        return hazardData;
    }

    private async Task<GeotechnicalData?> GetGeotechDataAsync(SiteLocation location, CancellationToken ct)
    {
        const double searchRadius = 500; // meters

        var boreholes = await _nzgdService.GetNearbyBoreholesAsync(location.Latitude, location.Longitude, searchRadius, ct);
        var cpts = await _nzgdService.GetNearbyCptsAsync(location.Latitude, location.Longitude, searchRadius, ct);
        var reports = await _nzgdService.GetNearbyReportsAsync(location.Latitude, location.Longitude, searchRadius, ct);

        return new GeotechnicalData
        {
            NearbyBoreholes = boreholes,
            NearbyCpts = cpts,
            NearbyReports = reports,
            GeotechInvestigationRequired = boreholes.Count == 0,
            RecommendedInvestigation = boreholes.Count == 0 
                ? "No nearby geotechnical data. Site-specific investigation recommended."
                : null,
            Source = new DataSource
            {
                SourceName = "NZ Geotechnical Database",
                SourceUrl = "https://www.nzgd.org.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow
            }
        };
    }

    private async Task<InfrastructureData?> GetInfrastructureDataAsync(SiteLocation location, CancellationToken ct)
    {
        var council = _councilServices.FirstOrDefault(c => c.SupportsRegion(location.Latitude, location.Longitude));
        
        if (council == null)
            return null;

        return await council.GetInfrastructureDataAsync(location.Latitude, location.Longitude, ct);
    }

    private async Task<ClimateData?> GetClimateDataAsync(SiteLocation location, CancellationToken ct)
    {
        var rainfall = await _niwaService.GetRainfallDataAsync(location.Latitude, location.Longitude, ct);
        var windZone = await _niwaService.GetWindZoneAsync(location.Latitude, location.Longitude, ct);

        return new ClimateData
        {
            WindZone = windZone,
            Rainfall = rainfall,
            Source = new DataSource
            {
                SourceName = "NIWA",
                SourceUrl = "https://cliflo.niwa.co.nz/",
                DataDate = DateTime.UtcNow,
                RetrievedDate = DateTime.UtcNow
            }
        };
    }

    private static DataCompleteness CalculateCompleteness(SiteEvaluation evaluation)
    {
        var completeness = new DataCompleteness();
        var sections = new Dictionary<string, SectionCompleteness>();

        // Check each section
        sections["Zoning"] = CheckSection(evaluation.Zoning != null, evaluation.Zoning?.Zone);
        sections["Hazards"] = CheckSection(evaluation.Hazards != null, evaluation.Hazards?.Flooding?.Zone);
        sections["Geotech"] = CheckSection(evaluation.Geotech != null, evaluation.Geotech?.NearbyBoreholes?.Count > 0);
        sections["Infrastructure"] = CheckSection(evaluation.Infrastructure != null, evaluation.Infrastructure?.Water?.Available);
        sections["Land"] = CheckSection(evaluation.Land != null, evaluation.Land?.TitleReference);
        sections["Climate"] = CheckSection(evaluation.Climate != null, evaluation.Climate?.WindZone);
        sections["Historical"] = CheckSection(evaluation.Historical != null, evaluation.Historical?.PreviousUse);

        completeness.Sections = sections;
        completeness.CompleteSections = sections.Values.Count(s => s.Status == CompletenessStatus.Complete);
        completeness.PartialSections = sections.Values.Count(s => s.Status == CompletenessStatus.Partial);
        completeness.MissingSections = sections.Values.Count(s => s.Status == CompletenessStatus.Missing);

        return completeness;
    }

    private static SectionCompleteness CheckSection(bool hasData, object? keyField)
    {
        if (!hasData)
            return new SectionCompleteness { Status = CompletenessStatus.Missing };
        
        if (keyField == null || (keyField is string s && string.IsNullOrEmpty(s)))
            return new SectionCompleteness { Status = CompletenessStatus.Partial };
        
        return new SectionCompleteness { Status = CompletenessStatus.Complete };
    }

    private static EvaluationStatus DetermineStatus(SiteEvaluation evaluation)
    {
        if (evaluation.DataGaps.Any(g => g.Severity == GapSeverity.Critical))
            return EvaluationStatus.RequiresManualData;
        
        if (evaluation.Completeness.CompletionPercentage >= 80)
            return EvaluationStatus.Complete;
        
        return EvaluationStatus.InProgress;
    }
}
