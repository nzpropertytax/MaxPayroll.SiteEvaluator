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
}

public class TemperatureData
{
    public double? AnnualMeanMax { get; set; }
    public double? AnnualMeanMin { get; set; }
    public int? HeatingDegreeDays { get; set; }
    public int? CoolingDegreeDays { get; set; }
}
