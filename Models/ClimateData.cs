namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Climate and environmental data from NIWA/MetService.
/// </summary>
public class ClimateData
{
    // === Wind Zone ===
    public string? WindZone { get; set; }  // Low, Medium, High, Very High, Extra High
    public double? BasicWindSpeed { get; set; }
    
    // === Detailed Wind Data ===
    public WindData? Wind { get; set; }
    
    // === Rainfall ===
    public RainfallData? Rainfall { get; set; }
    
    // === Temperature ===
    public TemperatureData? Temperature { get; set; }
    
    // === Climate Classification ===
    public string? ClimateZone { get; set; }
    
    public DataSource Source { get; set; } = new();
}

/// <summary>
/// Detailed wind data per NZS 3604:2011 and AS/NZS 1170.2.
/// </summary>
public class WindData
{
    /// <summary>NZS 3604 Wind Zone (Low, Medium, High, Very High, Extra High)</summary>
    public string WindZone { get; set; } = "Medium";
    
    /// <summary>V_R - Regional wind speed for the wind zone (m/s)</summary>
    public double BasicWindSpeed { get; set; }
    
    /// <summary>V_u - Ultimate limit state design wind speed (m/s)</summary>
    public double UltimateWindSpeed { get; set; }
    
    /// <summary>V_s - Serviceability limit state design wind speed (m/s)</summary>
    public double ServiceabilityWindSpeed { get; set; }
    
    /// <summary>M_d - Wind directional multiplier</summary>
    public double DirectionalMultiplier { get; set; } = 1.0;
    
    /// <summary>Terrain Category (TC1, TC2, TC2.5, TC3)</summary>
    public string? TerrainCategory { get; set; }
    
    /// <summary>M_s - Shielding multiplier</summary>
    public double ShieldingFactor { get; set; } = 1.0;
    
    /// <summary>M_t - Topographic multiplier for hills/escarpments</summary>
    public double TopographyFactor { get; set; } = 1.0;
    
    /// <summary>Predominant wind direction (e.g., "SW", "NW/S" for bi-modal)</summary>
    public string? PredominantDirection { get; set; }
    
    public DataSource? Source { get; set; }
    
    /// <summary>
    /// Calculate site wind speed including all multipliers.
    /// V_sit = V_R × M_d × (M_z,cat × M_s × M_t)
    /// </summary>
    public double CalculateSiteWindSpeed(double heightMultiplier = 1.0)
    {
        return BasicWindSpeed * DirectionalMultiplier * heightMultiplier * ShieldingFactor * TopographyFactor;
    }
}

public class RainfallData
{
    public double? AnnualMean { get; set; }
    public double? I10_10 { get; set; }  // 10-year 10-minute intensity
    public double? I10_60 { get; set; }  // 10-year 60-minute intensity
    public double? I100_10 { get; set; } // 100-year 10-minute intensity
    public double? I100_60 { get; set; } // 100-year 60-minute intensity
    public string? HirtdsStation { get; set; }
    
    /// <summary>
    /// Full HIRDS rainfall depths.
    /// Outer key: return period (e.g., "10yr", "100yr")
    /// Inner key: duration (e.g., "10min", "60min")
    /// Value: rainfall depth in mm
    /// </summary>
    public Dictionary<string, Dictionary<string, double>>? RainfallDepths { get; set; }
    
    /// <summary>
    /// Climate change adjustment factors.
    /// Key: scenario (e.g., "2090_RCP85")
    /// Value: multiplication factor
    /// </summary>
    public Dictionary<string, double>? ClimateChangeFactors { get; set; }
    
    public DataSource? Source { get; set; }
    
    /// <summary>
    /// Get rainfall depth (mm) for a given return period and duration.
    /// </summary>
    public double GetDepth(string returnPeriod, string duration)
    {
        if (RainfallDepths == null) return 0;
        if (!RainfallDepths.TryGetValue(returnPeriod, out var durations)) return 0;
        return durations.GetValueOrDefault(duration, 0);
    }
    
    /// <summary>
    /// Get rainfall intensity (mm/hr) for a given return period and duration.
    /// </summary>
    public double GetIntensity(string returnPeriod, string duration)
    {
        var depth = GetDepth(returnPeriod, duration);
        if (depth == 0) return 0;
        
        // Extract minutes from duration string (e.g., "60min" -> 60)
        var minutesStr = duration.Replace("min", "");
        if (!int.TryParse(minutesStr, out var minutes) || minutes == 0) return 0;
        
        return depth / minutes * 60; // Convert to mm/hr
    }
    
    /// <summary>
    /// Get climate-adjusted rainfall depth for future scenario.
    /// </summary>
    public double GetClimateAdjustedDepth(string returnPeriod, string duration, string scenario)
    {
        var baseDepth = GetDepth(returnPeriod, duration);
        var factor = ClimateChangeFactors?.GetValueOrDefault(scenario, 1.0) ?? 1.0;
        return baseDepth * factor;
    }
}

public class TemperatureData
{
    public double? AnnualMeanMax { get; set; }
    public double? AnnualMeanMin { get; set; }
    public double? MeanTemperature { get; set; }
    public int? FrostDaysPerYear { get; set; }
    public int? HeatingDegreeDays { get; set; }
    public int? CoolingDegreeDays { get; set; }
}
