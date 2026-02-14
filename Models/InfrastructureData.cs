namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Infrastructure and utility data.
/// </summary>
public class InfrastructureData
{
    // === Water Supply ===
    public WaterSupply? Water { get; set; }
    
    // === Wastewater ===
    public Wastewater? Wastewater { get; set; }
    
    // === Stormwater ===
    public Stormwater? Stormwater { get; set; }
    
    // === Power ===
    public PowerSupply? Power { get; set; }
    
    // === Communications ===
    public Communications? Communications { get; set; }
    
    // === Gas ===
    public GasSupply? Gas { get; set; }
    
    // === Roads ===
    public RoadAccess? Roads { get; set; }
    
    public DataSource Source { get; set; } = new();
}

public class WaterSupply
{
    public bool Available { get; set; }
    public string? Provider { get; set; }
    public string? MainSize { get; set; }
    public string? PressureZone { get; set; }
    public double? DistanceToMain { get; set; }
    public string? ConnectionNotes { get; set; }
    public string? Notes { get; set; }
}

public class Wastewater
{
    public bool Available { get; set; }
    public string? Provider { get; set; }
    public string? MainSize { get; set; }
    public string? MainDepth { get; set; }
    public double? DistanceToMain { get; set; }
    public string? Catchment { get; set; }
    public string? CapacityNotes { get; set; }
    public string? Notes { get; set; }
}

public class Stormwater
{
    public bool Available { get; set; }
    public string? Provider { get; set; }
    public string? MainSize { get; set; }
    public double? DistanceToMain { get; set; }
    public string? Catchment { get; set; }
    public bool OnSiteDisposalRequired { get; set; }
    public string? RequiredTreatment { get; set; }
    public string? DischargeConsent { get; set; }
    public string? Notes { get; set; }
}

public class PowerSupply
{
    public bool Available { get; set; }
    public string? Provider { get; set; }
    public string? NetworkArea { get; set; }
    public string? ConnectionNotes { get; set; }
}

public class Communications
{
    public bool FibreAvailable { get; set; }
    public string? FibreProvider { get; set; }
    public string? FibreType { get; set; }
    public bool CopperAvailable { get; set; }
}

public class GasSupply
{
    public bool Available { get; set; }
    public string? Provider { get; set; }
    public double? DistanceToMain { get; set; }
}

public class RoadAccess
{
    public string? RoadName { get; set; }
    public string? RoadClassification { get; set; }
    public string? RoadOwner { get; set; }
    public string? SpeedLimit { get; set; }
    public string? AccessNotes { get; set; }
}
