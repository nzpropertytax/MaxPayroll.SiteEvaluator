# Engineering Report Guide ??

<div align="center">

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   ?? PROFESSIONAL ENGINEERING REPORTS                            ?
?                                                                  ?
?   Comprehensive site evaluation reports for engineers            ?
?   to use and provide to their clients                           ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## ?? Purpose

The Site Evaluator produces professional engineering reports that:

- **Consolidate data** from multiple authoritative NZ sources
- **Present findings** in a format suitable for client deliverables
- **Identify risks** and highlight areas requiring further investigation
- **Support decisions** for property purchase, development, and consent

### Report Audience

| Primary | Secondary |
|---------|-----------|
| Civil Engineers | Property Developers |
| Geotechnical Engineers | Valuers |
| Structural Engineers | Real Estate Lawyers |
| Planning Consultants | Prospective Purchasers |

---

## ?? Report Types

### 1. Full Technical Report

**Purpose:** Comprehensive site assessment for professional use

**Contents:**
- Executive summary
- Property identification
- Full zoning analysis
- Complete hazard assessment
- Geotechnical data compilation
- Infrastructure assessment
- Climate data summary
- Data sources and methodology
- Limitations and disclaimers

**Best For:**
- Resource consent applications
- Detailed engineering assessments
- Client deliverables
- Due diligence records

### 2. Summary Report

**Purpose:** Quick overview of key findings

**Contents:**
- Property overview
- Key risk indicators
- Critical constraints identified
- Recommendations summary
- Next steps

**Best For:**
- Client briefings
- Initial feasibility discussions
- Quick reference

### 3. Geotechnical Focus Report

**Purpose:** Geotechnical data compilation

**Contents:**
- Site location and geology
- Liquefaction assessment
- Nearby investigation summary
- Foundation guidance
- Recommended investigations

**Best For:**
- Foundation design input
- Site investigation scoping
- Geotechnical practitioners

---

## ?? Report Sections

### Section 1: Property Identification

| Field | Source | Notes |
|-------|--------|-------|
| Address | LINZ Address API | Official formatted address |
| Title Reference | LINZ Landonline | CT reference if available |
| Legal Description | LINZ Landonline | Lot/DP, Section/Survey |
| Coordinates | LINZ/Search | WGS84 decimal degrees |
| Territorial Authority | LINZ | Council jurisdiction |
| Area | LINZ Parcel | Site area in m² |

### Section 2: Intended Use Assessment

| Field | Purpose |
|-------|---------|
| Use Category | Primary classification |
| Specific Use | Detailed intended use |
| Purpose | Why assessment was requested |
| Development Parameters | Height, coverage, units if new build |

### Section 3: Zoning Analysis

| Subsection | Content |
|------------|---------|
| Zone Classification | Zone name, code, district plan reference |
| Built Form Standards | Height, setbacks, coverage, impervious limits |
| Activity Status | Permitted/controlled/discretionary analysis |
| Overlays | All applicable overlays and their implications |
| Compatibility Assessment | Analysis against intended use |

### Section 4: Natural Hazards

| Hazard | Data Included |
|--------|---------------|
| Flooding | Zone, levels, floor requirements |
| Liquefaction | Category, description, foundation guidance |
| Seismic | Zone factor, site class, nearby faults |
| Contamination | HAIL/LLUR status, investigation needs |
| Other | Coastal, slope, subsidence, wildfire |

### Section 5: Geotechnical Data

| Data Type | Content |
|-----------|---------|
| Regional Geology | Underlying geology, soil types |
| Nearby Investigations | NZGD records within 500m |
| Foundation Guidance | Council technical category |
| Investigation Recommendations | Suggested scope based on findings |

### Section 6: Infrastructure

| Service | Information |
|---------|-------------|
| Water Supply | Connection, capacity, pressure |
| Wastewater | Sewer connection, capacity |
| Stormwater | Drainage, discharge requirements |
| Power | Network, capacity |
| Communications | Fibre availability |

### Section 7: Climate Data

| Parameter | Data |
|-----------|------|
| Wind Zone | NZS 3604 classification |
| Rainfall | Annual and design intensities |
| Snow Loading | If applicable |
| Coastal Exposure | Corrosion zone |

---

## ?? Using Reports in Practice

### For Client Deliverables

The report can be:
1. **Used directly** — Provide to clients as-is with your cover letter
2. **Incorporated** — Extract sections into your own report format
3. **Referenced** — Cite as data source in your assessment

### Professional Responsibility

**Remember:**
- Reports are **data compilations**, not engineering opinions
- Engineers must apply **professional judgment** to findings
- **Site-specific investigations** may still be required
- **Verify critical data** from primary sources for formal applications

### Recommended Workflow

```
1. Run Site Evaluation Wizard
         ?
2. Review findings, note gaps
         ?
3. Download appropriate report
         ?
4. Add professional interpretation
         ?
5. Commission additional investigations as needed
         ?
6. Prepare client deliverable
```

---

## ?? Example Report Extracts

### Executive Summary Example

```
SITE EVALUATION SUMMARY
=======================
Property: 123 Example Street, Christchurch 8011
Date: 15 January 2025
Evaluation ID: EVL-2025-001234

KEY FINDINGS:

? Zoning: Residential Medium Density Zone (RMD)
   - Intended use (townhouses) is PERMITTED

?? Liquefaction: Technical Category 2 (TC2)
   - Enhanced foundations likely required
   - Geotechnical investigation recommended

?? Flood Zone: Floor Management Area (FMA)
   - Minimum floor level: RL 12.50m
   - Flood assessment required for consent

? Infrastructure: All services available
   - Adequate capacity for development

RECOMMENDATIONS:
1. Commission geotechnical investigation (TC2)
2. Obtain flood level certificate from CCC
3. Proceed with resource consent preparation
```

### Hazard Summary Table Example

```
NATURAL HAZARD ASSESSMENT
=========================

| Hazard          | Status    | Risk Level | Action Required          |
|-----------------|-----------|------------|--------------------------|
| Flooding        | FMA Zone  | MEDIUM     | Floor level compliance   |
| Liquefaction    | TC2       | MEDIUM     | Geotech investigation    |
| Seismic         | Z=0.3     | STANDARD   | NZS 1170.5 design       |
| Contamination   | Not HAIL  | LOW        | No action required       |
| Slope Stability | N/A       | LOW        | Flat site               |
```

---

## ?? Limitations & Disclaimers

### Standard Report Disclaimer

All reports include:

```
IMPORTANT LIMITATIONS

This report compiles data from publicly available sources and is 
provided for information purposes only. The information should not 
be relied upon as a substitute for professional advice specific to 
your circumstances.

DATA CURRENCY:
- Data is retrieved at the time of evaluation
- Source databases are updated at varying frequencies
- Users should verify critical information with primary sources

PROFESSIONAL ADVICE:
- This report does not constitute engineering advice
- Professional interpretation is required for all findings
- Site-specific investigations may be required
- Consult qualified professionals for formal assessments

ACCURACY:
- Data accuracy depends on source database quality
- No warranty is given for completeness or accuracy
- Users accept responsibility for verifying critical data
```

### Engineer's Responsibility

When using these reports, engineers should:

| Do | Don't |
|----|-------|
| Apply professional judgment | Rely solely on report findings |
| Verify critical constraints | Assume data is complete |
| Commission investigations as needed | Skip site visits |
| Document limitations in client reports | Present as definitive assessment |
| Update assessments if conditions change | Use outdated reports for decisions |

---

## ?? Report Updates & Versioning

### When to Re-Run Evaluation

| Trigger | Reason |
|---------|--------|
| >3 months since evaluation | Data may have changed |
| District Plan change | Zone rules may have changed |
| New investigations nearby | Additional geotech data available |
| Council infrastructure upgrades | Capacity may have changed |
| Client requirements change | Different intended use |

### Report Versioning

Each report includes:
- Evaluation ID (unique identifier)
- Generation timestamp
- Data retrieval timestamps per section
- Source versions where available

---

## ?? Related Documentation

- [Site Evaluation Wizard Guide](Site-Evaluation-Wizard-Guide.md)
- [API Implementation Guides](../API-Implementation-Guides/README.md)
- [Data Sources Reference](Data-Sources-Reference.md)
- [AI Learning Model Guide](../development/AI-Learning-Model-Guide.md)

---

<div align="center">

**?? Professional Reports for Professional Engineers**

*Streamlining property due diligence in New Zealand*

</div>
