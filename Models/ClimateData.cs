namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Climate and environmental data from NIWA/MetService.
/// </summary>
public class ClimateData
{
    // === Wind Zone ===
    public string? WindZone { get; set; }  // Low, Medium, High, Very High
    public double? BasicWindSpeed { get; set; }
    
    // === Rainfall ===
    public RainfallData? Rainfall { get; set; }
    
    // === Temperature ===
    public TemperatureData? Temperature { get; set; }
    
    // === Climate Classification ===
    public string? ClimateZone { get; set; }
    
    public DataSource Source { get; set; } = new();
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
    /// Get rainfall intensity (mm/hr) for a given return period and duration.
    /// </summary>
    public double GetIntensity(string returnPeriod, string duration)
    {
        if (RainfallDepths == null) return 0;
        if (!RainfallDepths.TryGetValue(returnPeriod, out var durations)) return 0;
        if (!durations.TryGetValue(duration, out var depth)) return 0;
        
        // Extract minutes from duration string (e.g., "60min" -> 60)
        var minutes = int.Parse(duration.Replace("min", ""));
        return depth / minutes * 60; // Convert to mm/hr
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
