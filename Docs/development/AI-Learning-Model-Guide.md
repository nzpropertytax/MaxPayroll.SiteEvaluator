# AI Learning Model Guide ??

<div align="center">

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   ?? AI-POWERED SITE EVALUATION                                  ?
?                                                                  ?
?   Learning from historical data to improve recommendations       ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## ?? Overview

The Site Evaluator AI Learning Model improves evaluation quality over time by:

1. **Learning from historical evaluations** — Patterns in similar sites
2. **Incorporating engineer feedback** — Professional corrections and additions
3. **Tracking outcomes** — Results of consent applications and investigations
4. **Leveraging platform AI** — Integration with existing MaxPayroll AI services

### Value Proposition

| Without AI | With AI |
|------------|---------|
| Generic recommendations | Contextual suggestions based on similar sites |
| Manual gap identification | Automatic detection of missing critical data |
| Standard report text | AI-generated narratives specific to findings |
| No learning over time | Continuously improving recommendations |

---

## ??? Architecture

### System Integration

```
???????????????????????????????????????????????????????????????????
?                  MAXPAYROLL PLATFORM                             ?
?                                                                  ?
?  ????????????????????    ????????????????????                   ?
?  ? MaxPayroll.Website?    ? MaxPayroll.      ?                   ?
?  ? AI Learning      ?????? SiteEvaluator    ?                   ?
?  ? Services         ?    ? AI Module        ?                   ?
?  ????????????????????    ????????????????????                   ?
?          ?                        ?                              ?
?          ?                        ?                              ?
?  ????????????????????????????????????????????                   ?
?  ?           Shared AI Infrastructure        ?                   ?
?  ?  • Azure OpenAI / OpenAI API             ?                   ?
?  ?  • Embedding generation                   ?                   ?
?  ?  • Vector similarity search               ?                   ?
?  ????????????????????????????????????????????                   ?
???????????????????????????????????????????????????????????????????
```

### Data Flow

```
?????????????????????????????????????????????????????????????????
?                    LEARNING PIPELINE                           ?
?                                                                ?
?  ???????????    ????????????    ????????????    ??????????? ?
?  ?Evaluation? ?  ? Feature  ? ?  ? Embedding? ?  ? Vector  ? ?
?  ?Complete ?    ? Extract  ?    ? Generate ?    ? Store   ? ?
?  ???????????    ????????????    ????????????    ??????????? ?
?                                                      ?        ?
?                                                      ?        ?
?  ???????????    ????????????    ????????????    ??????????? ?
?  ? Suggest ? ?  ? Retrieve ? ?  ? Similarity? ?  ? Query   ? ?
?  ?         ?    ? Similar  ?    ? Search   ?    ? Embed   ? ?
?  ???????????    ????????????    ????????????    ??????????? ?
?????????????????????????????????????????????????????????????????
```

---

## ?? Feature Extraction

### Evaluation Features

Each completed evaluation generates a feature vector for AI learning:

```csharp
public class EvaluationFeatures
{
    // Location features
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TerritorialAuthority { get; set; }
    public string Suburb { get; set; }
    
    // Zoning features
    public string ZoneCode { get; set; }
    public double? MaxHeight { get; set; }
    public double? MaxCoverage { get; set; }
    public List<string> Overlays { get; set; }
    
    // Hazard features
    public string FloodZone { get; set; }
    public string LiquefactionCategory { get; set; }
    public double? SeismicZFactor { get; set; }
    public bool OnHail { get; set; }
    
    // Geotechnical features
    public int NearbyInvestigationCount { get; set; }
    public double? AverageDepthToRock { get; set; }
    public string DominantSoilType { get; set; }
    
    // Intended use features
    public PropertyUseCategory IntendedCategory { get; set; }
    public EvaluationPurpose Purpose { get; set; }
    public bool IsNewDevelopment { get; set; }
}
```

### Feature Importance

| Feature | Weight | Rationale |
|---------|--------|-----------|
| Location (TA, suburb) | High | Regional patterns in hazards/geology |
| Zone code | High | Similar zoning = similar constraints |
| Liquefaction category | High | Critical for foundation recommendations |
| Intended use | Medium | Affects relevant rule checking |
| Nearby investigations | Medium | Data availability indicator |

---

## ?? Similar Site Discovery

### Finding Comparable Properties

When a new evaluation starts, the AI finds similar completed evaluations:

```csharp
public interface ISiteEvaluatorAiService
{
    /// <summary>
    /// Find similar evaluations to provide context and recommendations.
    /// </summary>
    Task<List<SimilarEvaluation>> FindSimilarSitesAsync(
        EvaluationFeatures features, 
        int maxResults = 5);
    
    /// <summary>
    /// Generate recommendations based on similar sites.
    /// </summary>
    Task<List<AiRecommendation>> GenerateRecommendationsAsync(
        SiteEvaluation current,
        List<SimilarEvaluation> similar);
    
    /// <summary>
    /// Generate narrative text for report sections.
    /// </summary>
    Task<string> GenerateSectionNarrativeAsync(
        string section,
        SiteEvaluation evaluation,
        List<SimilarEvaluation> similar);
}

public class SimilarEvaluation
{
    public string EvaluationId { get; set; }
    public string Address { get; set; }
    public double SimilarityScore { get; set; }  // 0-1
    public EvaluationFeatures Features { get; set; }
    public EvaluationOutcome? Outcome { get; set; }
}
```

### Similarity Calculation

```csharp
public class SimilarityCalculator
{
    public double CalculateSimilarity(
        EvaluationFeatures a, 
        EvaluationFeatures b)
    {
        double score = 0;
        double totalWeight = 0;
        
        // Geographic similarity (0-1 based on distance)
        var distance = CalculateDistanceKm(
            a.Latitude, a.Longitude,
            b.Latitude, b.Longitude);
        score += (1 - Math.Min(distance / 50, 1)) * 3;  // Weight: 3
        totalWeight += 3;
        
        // Same territorial authority
        if (a.TerritorialAuthority == b.TerritorialAuthority)
        {
            score += 2;  // Weight: 2
        }
        totalWeight += 2;
        
        // Same zone code
        if (a.ZoneCode == b.ZoneCode)
        {
            score += 3;  // Weight: 3
        }
        totalWeight += 3;
        
        // Same liquefaction category
        if (a.LiquefactionCategory == b.LiquefactionCategory)
        {
            score += 2;  // Weight: 2
        }
        totalWeight += 2;
        
        // Same intended use category
        if (a.IntendedCategory == b.IntendedCategory)
        {
            score += 1;  // Weight: 1
        }
        totalWeight += 1;
        
        return score / totalWeight;
    }
}
```

---

## ?? AI-Generated Recommendations

### Recommendation Types

| Type | Source | Example |
|------|--------|---------|
| **Investigation** | Similar site outcomes | "Sites with TC2 in this area typically required deep boreholes" |
| **Risk Alert** | Pattern recognition | "3 of 5 similar sites had unexpected contamination findings" |
| **Constraint** | Rule learning | "Commercial developments in this zone often require acoustic assessment" |
| **Data Gap** | Comparison | "Consider obtaining flood level certificate - available for 4/5 similar sites" |

### Recommendation Generation

```csharp
public class AiRecommendation
{
    public string Category { get; set; }  // Investigation, Risk, Constraint, Gap
    public string Title { get; set; }
    public string Description { get; set; }
    public string Rationale { get; set; }
    public double Confidence { get; set; }  // 0-1
    public List<string> SupportingEvaluationIds { get; set; }
}
```

### Sample Output

```json
{
  "recommendations": [
    {
      "category": "Investigation",
      "title": "Enhanced Geotechnical Investigation Recommended",
      "description": "Consider deep boreholes to 15m+ based on nearby site outcomes.",
      "rationale": "4 of 5 similar TC2 sites in this area encountered variable ground conditions at depth, with 3 requiring enhanced foundation solutions.",
      "confidence": 0.85,
      "supportingEvaluationIds": ["EVL-001", "EVL-002", "EVL-003", "EVL-004"]
    },
    {
      "category": "Risk",
      "title": "Potential Floor Level Issue",
      "description": "Verify floor level requirement with council flood team.",
      "rationale": "2 similar sites had floor level requirements increased after detailed assessment.",
      "confidence": 0.65,
      "supportingEvaluationIds": ["EVL-002", "EVL-005"]
    }
  ]
}
```

---

## ?? Narrative Generation

### Report Section Narratives

AI generates contextual narrative text for report sections:

```csharp
public async Task<string> GenerateZoningSummaryAsync(
    SiteEvaluation evaluation)
{
    var prompt = $"""
        Generate a professional summary paragraph for a site evaluation report.
        
        Zone: {evaluation.Zoning?.Zone}
        Intended Use: {evaluation.IntendedUse?.Category}
        Key Constraints: {string.Join(", ", GetKeyConstraints(evaluation))}
        
        Write 2-3 sentences summarizing the zoning compatibility and key planning considerations.
        Use professional engineering language suitable for a client report.
        """;
    
    return await _aiService.GenerateTextAsync(prompt);
}
```

### Example Output

> "The subject site is located within the Residential Medium Density Zone (RMD) under the Christchurch District Plan. The proposed multi-unit residential development is a permitted activity subject to compliance with built form standards. Key considerations include the 8m height limit, 50% maximum coverage, and the Character Area Overlay which requires additional design assessment for street-facing elevations."

---

## ?? Feedback & Outcome Tracking

### Feedback Collection

Engineers can provide feedback on evaluations:

```csharp
public class EvaluationFeedback
{
    public string EvaluationId { get; set; }
    public string FeedbackType { get; set; }  // Correction, Addition, Confirmation
    public string Section { get; set; }
    public string Description { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; }
}
```

### Outcome Tracking

Track what happened after the evaluation:

```csharp
public class EvaluationOutcome
{
    public string EvaluationId { get; set; }
    
    // Resource consent outcome
    public bool? ConsentApplied { get; set; }
    public bool? ConsentGranted { get; set; }
    public string ConsentConditions { get; set; }
    
    // Geotechnical outcome
    public bool? GeotechInvestigationDone { get; set; }
    public string ActualFoundationType { get; set; }
    public bool? UnexpectedConditions { get; set; }
    public string UnexpectedConditionDetails { get; set; }
    
    // Development outcome
    public bool? DevelopmentProceeded { get; set; }
    public DateTime? CompletionDate { get; set; }
}
```

---

## ?? Platform Integration

### Leveraging Existing AI Services

The Site Evaluator AI integrates with MaxPayroll.Website's existing AI infrastructure:

```csharp
// In MaxPayroll.SiteEvaluator.Configuration

public static class SiteEvaluatorAiExtensions
{
    public static IServiceCollection AddSiteEvaluatorAi(
        this IServiceCollection services)
    {
        // Use platform's AI service
        services.AddScoped<ISiteEvaluatorAiService, SiteEvaluatorAiService>();
        
        // Feature extraction
        services.AddScoped<IFeatureExtractor, EvaluationFeatureExtractor>();
        
        // Similarity calculation  
        services.AddScoped<ISimilarityCalculator, SimilarityCalculator>();
        
        return services;
    }
}

public class SiteEvaluatorAiService : ISiteEvaluatorAiService
{
    private readonly IAiContentService _platformAi;  // From MaxPayroll.Website
    private readonly ISiteEvaluatorRepository _repository;
    private readonly IFeatureExtractor _featureExtractor;
    
    public SiteEvaluatorAiService(
        IAiContentService platformAi,
        ISiteEvaluatorRepository repository,
        IFeatureExtractor featureExtractor)
    {
        _platformAi = platformAi;
        _repository = repository;
        _featureExtractor = featureExtractor;
    }
    
    // Implementation...
}
```

---

## ?? Implementation Roadmap

### Phase 1: Data Collection (Current)

| Task | Status |
|------|--------|
| Feature extraction on evaluation completion | ?? In Progress |
| Store evaluation features in database | ?? Planned |
| Basic similarity calculation | ?? Planned |

### Phase 2: Pattern Recognition

| Task | Status |
|------|--------|
| Vector embeddings for evaluations | ?? Planned |
| Similar site search API | ?? Planned |
| Basic recommendation generation | ?? Planned |

### Phase 3: Recommendation Engine

| Task | Status |
|------|--------|
| AI-powered recommendations | ?? Planned |
| Confidence scoring | ?? Planned |
| Supporting evidence links | ?? Planned |

### Phase 4: Narrative Generation

| Task | Status |
|------|--------|
| Section narrative generation | ?? Planned |
| Report customization | ?? Planned |
| Executive summary generation | ?? Planned |

### Phase 5: Feedback Loop

| Task | Status |
|------|--------|
| Feedback collection UI | ?? Planned |
| Outcome tracking | ?? Planned |
| Model retraining pipeline | ?? Planned |

---

## ?? Related Documentation

- [API Implementation Guides](../API-Implementation-Guides/README.md)
- [Engineering Report Guide](../user-guides/Engineering-Report-Guide.md)
- [Service Implementation Guide](Service-Implementation-Guide.md)

---

<div align="center">

**?? AI That Learns From Every Evaluation**

*Smarter recommendations, better outcomes*

</div>
