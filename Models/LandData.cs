namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// LINZ land and title information.
/// </summary>
public class LandData
{
    // === Title Information ===
    public string? TitleReference { get; set; }
    public string? TitleType { get; set; }
    public string? TitleStatus { get; set; }
    public DateTime? TitleDate { get; set; }
    
    // === Legal Description ===
    public string? LegalDescription { get; set; }
    public string? LotNumber { get; set; }
    public string? DpNumber { get; set; }
    public double? AreaHectares { get; set; }
    public double? AreaSquareMeters { get; set; }
    
    // === Ownership ===
    public List<Owner> Owners { get; set; } = [];
    
    // === Encumbrances ===
    public List<Easement> Easements { get; set; } = [];
    public List<Covenant> Covenants { get; set; } = [];
    public List<Encumbrance> OtherEncumbrances { get; set; } = [];
    
    // === Survey Information ===
    public List<SurveyPlan> SurveyPlans { get; set; } = [];
    
    public DataSource Source { get; set; } = new();
}

public class Owner
{
    public string Name { get; set; } = string.Empty;
    public string? Share { get; set; }
}

public class Easement
{
    public string Type { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string? InFavourOf { get; set; }
    public string? Area { get; set; }
    public string? DocumentReference { get; set; }
}

public class Covenant
{
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentReference { get; set; }
}

public class Encumbrance
{
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentReference { get; set; }
}

public class SurveyPlan
{
    public string Reference { get; set; } = string.Empty;
    public string? Type { get; set; }
    public DateTime? Date { get; set; }
    public string? ViewUrl { get; set; }
}
