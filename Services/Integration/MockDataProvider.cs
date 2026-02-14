using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services.Integration;

/// <summary>
/// Mock data provider for development and testing when API keys are not configured.
/// Uses real NZ addresses with realistic property data.
/// 
/// PRIMARY CASE STUDY: 353 Barbadoes Street, Christchurch
/// Real property data from Christchurch City Council website.
/// Use case: Multi-unit residential development (block of flats)
/// </summary>
public static class MockDataProvider
{
    /// <summary>
    /// Sample NZ addresses for testing. Add more as needed.
    /// </summary>
    public static readonly List<MockAddress> SampleAddresses = new()
    {
        // ============================================================
        // PRIMARY CASE STUDY: 353 Barbadoes Street, Christchurch
        // REAL DATA from Christchurch City Council website
        // Proposed: Multi-storey block of residential flats (2-4 levels)
        // ============================================================
        new MockAddress
        {
            // Real address from CCC
            FullAddress = "353 Barbadoes Street, Central City, Christchurch 8011",
            Suburb = "Central City",
            City = "Christchurch",
            Latitude = -43.5270,
            Longitude = 172.6420,
            TerritorialAuthority = "Christchurch City Council",
            
            // Real legal/valuation data from CCC
            LegalDescription = "Pt Sec 509 Christchurch Town",
            TitleReference = "CB32A/891",  // Example - would need LINZ lookup for real
            AreaSqm = 177,  // 0.0177 hectares = 177 m²
            ValuationNumber = "22710 21300",
            RateAccountNumber = "73028090",
            
            // Real valuation data (as at 1 August 2022)
            LandValue = 280000,
            ImprovementsValue = 60000,
            CapitalValue = 340000,
            
            // Real rates data (2025/2026)
            CurrentYearRates = 2302.22m,
            RatesInstalment1 = 575.49m,
            RatesInstalment2 = 575.49m,
            RatesInstalment3 = 575.49m,
            RatesInstalment4 = 575.75m,
            RatingYear = "2025/2026",
            
            // Zoning - Central City zone (typical for this location)
            Zoning = "Central City Residential",
            ZoneCode = "CCR",
            MaxHeight = 14.0,
            MaxCoverage = 50,
            
            // Hazard data from engineering report
            LiquefactionCategory = "TC2",
            LiquefactionDescription = "Moderate liquefaction vulnerability - specific engineering assessment likely required",
            FloodZone = "Low-Moderate",
            FloodNotes = "Localised ponding possible along parts of Barbadoes Street",
            SeismicZone = "High",
            WindZone = "Medium-High",
            
            // Geotechnical from CGD
            SiteClass = "D",
            GroundwaterDepth = 1.5,
            SoilDescription = "Silty sands and gravels with variable sands and occasional silt/peat lenses",
            
            // Infrastructure
            WaterAvailable = true,
            WastewaterAvailable = true,
            StormwaterAvailable = true,
            StormwaterNotes = "On-site attenuation required due to shallow groundwater and limited soakage",
            PowerAvailable = true,
            FibreAvailable = true,
            
            // Development context
            ProposedUse = "Multi-unit residential (block of flats)",
            DevelopmentNotes = "2-4 storey residential, on-site parking, stormwater attenuation required. Small site (177m²) may limit development options."
        },

        // Other Christchurch addresses for variety
        new MockAddress
        {
            FullAddress = "90 Armagh Street, Christchurch Central, Christchurch 8011",
            Suburb = "Christchurch Central",
            City = "Christchurch",
            Latitude = -43.5301,
            Longitude = 172.6353,
            TerritorialAuthority = "Christchurch City Council",
            LegalDescription = "Lot 1 DP 12345",
            TitleReference = "CB45A/123",
            AreaSqm = 450,
            Zoning = "Commercial Core",
            LiquefactionCategory = "TC2",
            SeismicZone = "High"
        },
        new MockAddress
        {
            FullAddress = "1 Worcester Boulevard, Christchurch Central, Christchurch 8013",
            Suburb = "Christchurch Central",
            City = "Christchurch",
            Latitude = -43.5330,
            Longitude = 172.6365,
            TerritorialAuthority = "Christchurch City Council",
            LegalDescription = "Lot 2 DP 45678",
            TitleReference = "CB52B/456",
            Zoning = "Central City Mixed Use",
            LiquefactionCategory = "TC2",
            SeismicZone = "High"
        },
        new MockAddress
        {
            FullAddress = "Unit 1, 123 Manchester Street, Christchurch Central, Christchurch 8011",
            Suburb = "Christchurch Central",
            City = "Christchurch",
            Latitude = -43.5315,
            Longitude = 172.6378,
            TerritorialAuthority = "Christchurch City Council",
            LegalDescription = "Unit 1 DP 54321",
            TitleReference = "CB88C/789",
            AreaSqm = 85,
            Zoning = "Residential Medium Density",
            LiquefactionCategory = "TC2"
        },
        
        // Auckland address
        new MockAddress
        {
            FullAddress = "1 Queen Street, Auckland CBD, Auckland 1010",
            Suburb = "Auckland CBD",
            City = "Auckland",
            Latitude = -36.8485,
            Longitude = 174.7633,
            TerritorialAuthority = "Auckland Council",
            LegalDescription = "Lot 1 DP 99999",
            TitleReference = "NA123/789",
            Zoning = "Business - City Centre",
            LiquefactionCategory = "TC1",
            SeismicZone = "Medium"
        },
        
        // Wellington address
        new MockAddress
        {
            FullAddress = "1 Willis Street, Wellington Central, Wellington 6011",
            Suburb = "Wellington Central",
            City = "Wellington",
            Latitude = -41.2865,
            Longitude = 174.7762,
            TerritorialAuthority = "Wellington City Council",
            LegalDescription = "Lot 5 DP 33333",
            TitleReference = "WN45C/321",
            Zoning = "Central Area",
            LiquefactionCategory = "TC1",
            SeismicZone = "Very High"
        }
    };

    /// <summary>
    /// Get address suggestions matching a query (mock implementation).
    /// </summary>
    public static List<AddressSuggestion> GetAddressSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<AddressSuggestion>();

        var lowerQuery = query.ToLowerInvariant();
        
        return SampleAddresses
            .Where(a => 
                a.FullAddress.ToLowerInvariant().Contains(lowerQuery) ||
                a.Suburb?.ToLowerInvariant().Contains(lowerQuery) == true ||
                a.City?.ToLowerInvariant().Contains(lowerQuery) == true ||
                // Also match street numbers
                a.FullAddress.ToLowerInvariant().StartsWith(lowerQuery))
            .Select(a => new AddressSuggestion
            {
                FullAddress = a.FullAddress,
                Suburb = a.Suburb,
                City = a.City,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            })
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Get a site location from a mock address.
    /// </summary>
    public static SiteLocation? GetSiteLocation(string address)
    {
        var lowerAddress = address.ToLowerInvariant();
        
        var match = SampleAddresses.FirstOrDefault(a => 
            a.FullAddress.ToLowerInvariant().Contains(lowerAddress) ||
            lowerAddress.Contains(a.FullAddress.ToLowerInvariant().Split(',')[0]) ||
            // Match partial address (e.g., "353 Barbadoes")
            (lowerAddress.Length > 5 && a.FullAddress.ToLowerInvariant().Contains(lowerAddress.Substring(0, Math.Min(15, lowerAddress.Length)))));
        
        if (match == null)
            return null;

        return new SiteLocation
        {
            Address = match.FullAddress,
            Latitude = match.Latitude,
            Longitude = match.Longitude,
            LegalDescription = match.LegalDescription ?? "Mock Legal Description",
            TerritorialAuthority = match.TerritorialAuthority,
            Suburb = match.Suburb,
            City = match.City
        };
    }

    /// <summary>
    /// Get mock property matches for Step 2.
    /// </summary>
    public static List<PropertyMatch> GetPropertyMatches(string address)
    {
        var location = GetSiteLocation(address);
        if (location == null)
            return new List<PropertyMatch>();

        var matchingAddress = SampleAddresses.FirstOrDefault(a => 
            a.FullAddress.Equals(location.Address, StringComparison.OrdinalIgnoreCase));

        if (matchingAddress == null)
            return new List<PropertyMatch>();

        return new List<PropertyMatch>
        {
            new PropertyMatch
            {
                TitleReference = matchingAddress.TitleReference ?? "MOCK/123",
                LegalDescription = matchingAddress.LegalDescription ?? "Mock Legal Description",
                Address = matchingAddress.FullAddress,
                AreaSquareMeters = matchingAddress.AreaSqm ?? 500,
                TitleType = "Freehold",
                MatchScore = 95,
                Owners = new List<string> { "Mock Owner" },
                Source = "Mock Data (Development Mode)"
            }
        };
    }

    /// <summary>
    /// Get the primary case study address (353 Barbadoes Street).
    /// </summary>
    public static MockAddress GetPrimaryCaseStudy()
    {
        return SampleAddresses.First(a => a.FullAddress.Contains("353 Barbadoes"));
    }

    /// <summary>
    /// Add a custom test address to the mock data.
    /// </summary>
    public static void AddTestAddress(MockAddress address)
    {
        // Check if already exists
        if (!SampleAddresses.Any(a => a.FullAddress.Equals(address.FullAddress, StringComparison.OrdinalIgnoreCase)))
        {
            SampleAddresses.Add(address);
        }
    }
}

/// <summary>
/// Mock address data structure with comprehensive engineering and council data.
/// </summary>
public class MockAddress
{
    // Basic address info
    public string FullAddress { get; set; } = "";
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? TerritorialAuthority { get; set; }
    public string? LegalDescription { get; set; }
    public string? TitleReference { get; set; }
    public double? AreaSqm { get; set; }
    
    // Council/Valuation data
    public string? ValuationNumber { get; set; }
    public string? RateAccountNumber { get; set; }
    public decimal? LandValue { get; set; }
    public decimal? ImprovementsValue { get; set; }
    public decimal? CapitalValue { get; set; }
    
    // Rates data
    public decimal? CurrentYearRates { get; set; }
    public decimal? RatesInstalment1 { get; set; }
    public decimal? RatesInstalment2 { get; set; }
    public decimal? RatesInstalment3 { get; set; }
    public decimal? RatesInstalment4 { get; set; }
    public string? RatingYear { get; set; }
    
    // Zoning
    public string? Zoning { get; set; }
    public string? ZoneCode { get; set; }
    public double? MaxHeight { get; set; }
    public int? MaxCoverage { get; set; }
    
    // Hazards
    public string? LiquefactionCategory { get; set; }
    public string? LiquefactionDescription { get; set; }
    public string? FloodZone { get; set; }
    public string? FloodNotes { get; set; }
    public string? SeismicZone { get; set; }
    public string? WindZone { get; set; }
    
    // Geotechnical
    public string? SiteClass { get; set; }
    public double? GroundwaterDepth { get; set; }
    public string? SoilDescription { get; set; }
    
    // Infrastructure
    public bool? WaterAvailable { get; set; }
    public bool? WastewaterAvailable { get; set; }
    public bool? StormwaterAvailable { get; set; }
    public string? StormwaterNotes { get; set; }
    public bool? PowerAvailable { get; set; }
    public bool? FibreAvailable { get; set; }
    
    // Development context
    public string? ProposedUse { get; set; }
    public string? DevelopmentNotes { get; set; }
}

/// <summary>
/// Property match result for Step 2.
/// </summary>
public class PropertyMatch
{
    public string TitleReference { get; set; } = "";
    public string? LegalDescription { get; set; }
    public string? Address { get; set; }
    public double? AreaSquareMeters { get; set; }
    public string? TitleType { get; set; }
    public int MatchScore { get; set; }
    public List<string>? Owners { get; set; }
    public string? Source { get; set; }
}
