using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Models.Wizard;
using MaxPayroll.SiteEvaluator.Services.Integration;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Manages property locations with cached data.
/// </summary>
public class LocationService : ILocationService
{
    private readonly ISiteEvaluatorRepository _repository;
    private readonly ILinzDataService _linzService;
    private readonly IEnumerable<ICouncilDataService> _councilServices;
    private readonly IGnsDataService _gnsService;
    private readonly INiwaDataService _niwaService;
    private readonly INzgdDataService _nzgdService;
    private readonly ILogger<LocationService> _logger;

    private const int CacheMaxAgeHours = 24;
    private const double NearbyRadiusMeters = 50;

    public LocationService(
        ISiteEvaluatorRepository repository,
        ILinzDataService linzService,
        IEnumerable<ICouncilDataService> councilServices,
        IGnsDataService gnsService,
        INiwaDataService niwaService,
        INzgdDataService nzgdService,
        ILogger<LocationService> logger)
    {
        _repository = repository;
        _linzService = linzService;
        _councilServices = councilServices;
        _gnsService = gnsService;
        _niwaService = niwaService;
        _nzgdService = nzgdService;
        _logger = logger;
    }

    public async Task<PropertyLocation> GetOrCreateByAddressAsync(string address, CancellationToken ct = default)
    {
        _logger.LogInformation("Looking up location by address: {Address}", address);

        // Look up address via LINZ
        var siteLocation = await _linzService.LookupAddressAsync(address, ct);
        if (siteLocation == null)
        {
            throw new ArgumentException($"Address not found: {address}");
        }

        // Check for existing location nearby
        var existing = await FindNearbyLocationsAsync(siteLocation.Latitude, siteLocation.Longitude, NearbyRadiusMeters, ct);
        var match = existing.FirstOrDefault(l => 
            l.Address.Equals(siteLocation.Address, StringComparison.OrdinalIgnoreCase) ||
            (l.TitleReference != null && l.TitleReference == siteLocation.TitleReference));

        if (match != null)
        {
            _logger.LogInformation("Found existing location {LocationId} for address", match.Id);
            return match;
        }

        // Create new location
        var location = new PropertyLocation
        {
            Address = siteLocation.Address,
            TitleReference = siteLocation.TitleReference,
            LegalDescription = siteLocation.LegalDescription,
            Latitude = siteLocation.Latitude,
            Longitude = siteLocation.Longitude,
            Suburb = siteLocation.Suburb,
            City = siteLocation.City,
            TerritorialAuthority = siteLocation.TerritorialAuthority,
            RegionalCouncil = siteLocation.RegionalCouncil,
            Boundary = siteLocation.Boundary,
            Source = "LINZ",
            GeocodeConfidence = 95
        };

        // Parse address components
        ParseAddressComponents(location, siteLocation.Address);

        await _repository.InsertAsync(location);
        _logger.LogInformation("Created new location {LocationId} for {Address}", location.Id, address);

        return location;
    }

    public async Task<PropertyLocation> GetOrCreateByTitleAsync(string titleReference, CancellationToken ct = default)
    {
        _logger.LogInformation("Looking up location by title: {Title}", titleReference);

        // Check for existing location with this title
        var existing = await _repository.FindAsync<PropertyLocation>(l => l.TitleReference == titleReference);
        var match = existing.FirstOrDefault();

        if (match != null)
        {
            _logger.LogInformation("Found existing location {LocationId} for title", match.Id);
            return match;
        }

        // Look up via LINZ Landonline
        var landData = await _linzService.GetTitleDataAsync(titleReference, ct);
        if (landData == null)
        {
            throw new ArgumentException($"Title not found: {titleReference}");
        }

        // Create new location
        var location = new PropertyLocation
        {
            TitleReference = titleReference,
            LegalDescription = landData.LegalDescription,
            Address = $"Title: {titleReference}",
            SiteAreaM2 = landData.AreaSquareMeters,
            Source = "LINZ Landonline"
        };

        await _repository.InsertAsync(location);
        _logger.LogInformation("Created new location {LocationId} for title {Title}", location.Id, titleReference);

        return location;
    }

    public async Task<PropertyLocation> GetOrCreateByCoordinatesAsync(double lat, double lon, CancellationToken ct = default)
    {
        _logger.LogInformation("Looking up location by coordinates: {Lat}, {Lon}", lat, lon);

        // Check for existing nearby location
        var existing = await FindNearbyLocationsAsync(lat, lon, NearbyRadiusMeters, ct);
        if (existing.Any())
        {
            var nearest = existing.First();
            _logger.LogInformation("Found existing nearby location {LocationId}", nearest.Id);
            return nearest;
        }

        // Reverse geocode to get address
        // (In a real implementation, we'd call a reverse geocoding API)
        var location = new PropertyLocation
        {
            Address = $"Location at {lat:F6}, {lon:F6}",
            Latitude = lat,
            Longitude = lon,
            Source = "Coordinates",
            GeocodeConfidence = 50 // Lower confidence for coordinate-based entry
        };

        await _repository.InsertAsync(location);
        _logger.LogInformation("Created new location {LocationId} from coordinates", location.Id);

        return location;
    }

    public async Task<PropertyLocation?> GetLocationAsync(string locationId, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync<PropertyLocation>(locationId);
    }

    public async Task<IEnumerable<PropertyLocation>> FindNearbyLocationsAsync(double lat, double lon, double radiusMeters = 50, CancellationToken ct = default)
    {
        // Simple distance-based search
        // In production, this would use a spatial index
        var all = await _repository.FindAsync<PropertyLocation>(_ => true);
        
        return all
            .Where(l => GeoUtils.CalculateDistance(lat, lon, l.Latitude, l.Longitude) <= radiusMeters)
            .OrderBy(l => GeoUtils.CalculateDistance(lat, lon, l.Latitude, l.Longitude))
            .ToList();
    }

    public async Task<PropertyLocation> RefreshLocationDataAsync(string locationId, IEnumerable<string>? sections = null, CancellationToken ct = default)
    {
        var location = await _repository.GetByIdAsync<PropertyLocation>(locationId)
            ?? throw new ArgumentException($"Location not found: {locationId}");

        var sectionsToRefresh = sections?.Select(s => s.ToLowerInvariant()).ToList()
            ?? new List<string> { "zoning", "hazards", "geotech", "infrastructure", "climate", "land" };

        _logger.LogInformation("Refreshing data for location {LocationId}, sections: {Sections}",
            locationId, string.Join(", ", sectionsToRefresh));

        var tasks = new List<Task>();

        if (sectionsToRefresh.Contains("zoning") && location.IsCacheStale("zoning", CacheMaxAgeHours))
        {
            tasks.Add(RefreshZoningAsync(location, ct));
        }

        if (sectionsToRefresh.Contains("hazards") && location.IsCacheStale("hazards", CacheMaxAgeHours))
        {
            tasks.Add(RefreshHazardsAsync(location, ct));
        }

        if (sectionsToRefresh.Contains("geotech") && location.IsCacheStale("geotech", CacheMaxAgeHours))
        {
            tasks.Add(RefreshGeotechAsync(location, ct));
        }

        if (sectionsToRefresh.Contains("infrastructure") && location.IsCacheStale("infrastructure", CacheMaxAgeHours))
        {
            tasks.Add(RefreshInfrastructureAsync(location, ct));
        }

        if (sectionsToRefresh.Contains("climate") && location.IsCacheStale("climate", CacheMaxAgeHours))
        {
            tasks.Add(RefreshClimateAsync(location, ct));
        }

        if (sectionsToRefresh.Contains("land") && location.IsCacheStale("land", CacheMaxAgeHours))
        {
            tasks.Add(RefreshLandAsync(location, ct));
        }

        await Task.WhenAll(tasks);

        location.LastUpdated = DateTime.UtcNow;
        await _repository.UpdateAsync(location);

        return location;
    }

    public async Task<IEnumerable<LocationSummary>> GetLocationSummariesAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var locations = await _repository.FindAsync<PropertyLocation>(_ => true);
        var jobs = await _repository.FindAsync<EvaluationJob>(_ => true);

        var jobsByLocation = jobs.GroupBy(j => j.LocationId).ToDictionary(g => g.Key, g => g.ToList());

        return locations
            .OrderByDescending(l => l.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(l =>
            {
                var locationJobs = jobsByLocation.GetValueOrDefault(l.Id, []);
                return new LocationSummary
                {
                    Id = l.Id,
                    Address = l.Address,
                    TitleReference = l.TitleReference,
                    Suburb = l.Suburb,
                    City = l.City,
                    TerritorialAuthority = l.TerritorialAuthority,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    JobCount = locationJobs.Count,
                    LastJobDate = locationJobs.MaxBy(j => j.CreatedDate)?.CreatedDate
                };
            })
            .ToList();
    }

    // === Private refresh methods ===

    private async Task RefreshZoningAsync(PropertyLocation location, CancellationToken ct)
    {
        try
        {
            var council = _councilServices.FirstOrDefault(c => c.SupportsRegion(location.Latitude, location.Longitude));
            if (council != null)
            {
                location.CachedZoning = await council.GetZoningDataAsync(location.Latitude, location.Longitude, ct);
                location.ZoningCachedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh zoning for location {LocationId}", location.Id);
        }
    }

    private async Task RefreshHazardsAsync(PropertyLocation location, CancellationToken ct)
    {
        try
        {
            var council = _councilServices.FirstOrDefault(c => c.SupportsRegion(location.Latitude, location.Longitude));
            if (council != null)
            {
                var hazards = await council.GetHazardDataAsync(location.Latitude, location.Longitude, ct);
                if (hazards != null)
                {
                    // Add seismic data from GNS
                    hazards.Seismic = await _gnsService.GetSeismicHazardAsync(location.Latitude, location.Longitude, ct);
                }
                location.CachedHazards = hazards;
                location.HazardsCachedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh hazards for location {LocationId}", location.Id);
        }
    }

    private async Task RefreshGeotechAsync(PropertyLocation location, CancellationToken ct)
    {
        try
        {
            const double searchRadius = 500;
            var boreholes = await _nzgdService.GetNearbyBoreholesAsync(location.Latitude, location.Longitude, searchRadius, ct);
            var cpts = await _nzgdService.GetNearbyCptsAsync(location.Latitude, location.Longitude, searchRadius, ct);
            var reports = await _nzgdService.GetNearbyReportsAsync(location.Latitude, location.Longitude, searchRadius, ct);

            location.CachedGeotech = new GeotechnicalData
            {
                NearbyBoreholes = boreholes,
                NearbyCpts = cpts,
                NearbyReports = reports,
                GeotechInvestigationRequired = boreholes.Count == 0,
                Source = new DataSource
                {
                    SourceName = "NZ Geotechnical Database",
                    SourceUrl = "https://www.nzgd.org.nz/",
                    RetrievedDate = DateTime.UtcNow
                }
            };
            location.GeotechCachedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh geotech for location {LocationId}", location.Id);
        }
    }

    private async Task RefreshInfrastructureAsync(PropertyLocation location, CancellationToken ct)
    {
        try
        {
            var council = _councilServices.FirstOrDefault(c => c.SupportsRegion(location.Latitude, location.Longitude));
            if (council != null)
            {
                location.CachedInfrastructure = await council.GetInfrastructureDataAsync(location.Latitude, location.Longitude, ct);
                location.InfrastructureCachedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh infrastructure for location {LocationId}", location.Id);
        }
    }

    private async Task RefreshClimateAsync(PropertyLocation location, CancellationToken ct)
    {
        try
        {
            var rainfall = await _niwaService.GetRainfallDataAsync(location.Latitude, location.Longitude, ct);
            var windZone = await _niwaService.GetWindZoneAsync(location.Latitude, location.Longitude, ct);

            location.CachedClimate = new ClimateData
            {
                WindZone = windZone,
                Rainfall = rainfall,
                Source = new DataSource
                {
                    SourceName = "NIWA",
                    SourceUrl = "https://cliflo.niwa.co.nz/",
                    RetrievedDate = DateTime.UtcNow
                }
            };
            location.ClimateCachedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh climate for location {LocationId}", location.Id);
        }
    }

    private async Task RefreshLandAsync(PropertyLocation location, CancellationToken ct)
    {
        try
        {
            if (!string.IsNullOrEmpty(location.TitleReference))
            {
                location.CachedLand = await _linzService.GetTitleDataAsync(location.TitleReference, ct);
                location.LandCachedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh land data for location {LocationId}", location.Id);
        }
    }

    // === Helpers ===

    private static void ParseAddressComponents(PropertyLocation location, string address)
    {
        // Simple address parsing - in production use a proper parser
        var parts = address.Split(',').Select(p => p.Trim()).ToArray();
        if (parts.Length >= 1)
        {
            var streetParts = parts[0].Split(' ');
            if (streetParts.Length >= 2 && int.TryParse(streetParts[0], out _))
            {
                location.StreetNumber = streetParts[0];
                location.StreetName = string.Join(' ', streetParts.Skip(1));
            }
        }
    }

    private static Coordinate CalculateCentroid(List<Coordinate> boundary)
    {
        if (boundary.Count == 0) return new Coordinate();
        
        var lat = boundary.Average(c => c.Latitude);
        var lon = boundary.Average(c => c.Longitude);
        return new Coordinate { Latitude = lat, Longitude = lon };
    }
}
