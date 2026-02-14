# Case Study: 360 Barbadoes Street, Christchurch

**Project Type:** Multi-Unit Residential Development (Block of Flats)  
**Status:** ?? Primary Test Case for Site Evaluator  
**Last Updated:** January 2025

---

## ?? Site Details

| Property | Value |
|----------|-------|
| **Address** | 360 Barbadoes Street, Christchurch Central, Christchurch 8011 |
| **Legal Description** | Lot 1 DP 78542 |
| **Title Reference** | CB32A/891 |
| **Site Area** | ~850 m² |
| **Coordinates** | -43.5270, 172.6420 |
| **Territorial Authority** | Christchurch City Council |

---

## ??? Proposed Development

### Development Concept
- **Type:** Multi-storey block of residential flats (2–4 levels anticipated)
- **Units:** Multiple residential units
- **Parking:** On-site parking and access
- **Stormwater:** Attenuation and controlled discharge required
- **Services:** Connection to existing CCC wastewater and water supply networks

### Site Context
The site is located within an established residential/commercial fringe area with increasing redevelopment activity. Surrounding properties include a mix of older villas, infill townhouses, and small commercial units.

---

## ??? Zoning & Planning

| Rule | Value |
|------|-------|
| **Zone** | Residential Medium Density (RMD) |
| **Zone Code** | RMD |
| **Max Height** | 14 m |
| **Max Site Coverage** | 50% |
| **Parking Requirement** | 1 space per unit (varies with unit size and proximity to PT) |
| **District Plan** | [Christchurch District Plan](https://districtplan.ccc.govt.nz/) |

### Key Planning Considerations
- Multi-storey residential development is a permitted activity in RMD zone
- Resource consent may be required if triggered by height, density, or parking rules
- Vehicle access must comply with AS/NZS 2890.1 (Off-street Parking)

---

## ?? Natural Hazards

### Liquefaction Assessment

| Parameter | Value | Risk Level |
|-----------|-------|------------|
| **Category** | TC2 | ?? Moderate |
| **Description** | Moderate liquefaction vulnerability |
| **Assessment Required** | Specific engineering assessment likely required |

### Flooding

| Parameter | Value | Risk Level |
|-----------|-------|------------|
| **Zone** | Low–Moderate | ?? Moderate |
| **Notes** | Localised ponding possible along parts of Barbadoes Street |

### Seismic

| Parameter | Value |
|-----------|-------|
| **Seismic Zone** | High |
| **Wind Zone** | Medium–High |
| **Design Standards** | NZS 1170.5, B1/VM1, B1/AS1 |

### Hazard Summary

| Hazard | Status |
|--------|--------|
| Liquefaction | ?? Moderate |
| Flooding | ?? Low–Moderate (localised ponding possible) |
| Tsunami | ?? Negligible (inland) |
| Land Instability | ?? Low |
| Contamination | ?? Possible due to historic fill in central Christchurch |

---

## ?? Geotechnical Data

### Ground Conditions (from CGD)

| Parameter | Value |
|-----------|-------|
| **Site Class** | D |
| **Groundwater Depth** | 1.0–2.0 m below ground level |
| **Surface Soils** | Silty sands and gravels |
| **Intermediate Layers** | Variable sands with occasional silt/peat lenses |

### Geotechnical Risks

| Risk | Assessment |
|------|------------|
| Liquefaction | Moderate |
| Lateral Spread | Low (distance from major waterways) |
| Settlement | Possible under seismic loading |
| Bearing Capacity | Moderate, to be confirmed |

### Recommended Foundation Systems

Based on expected ground conditions, suitable foundation systems may include:

1. **Engineered raft slab**
2. **Deepened edge beams**
3. **Screw piles or driven timber piles** (if liquefaction layers confirmed)
4. **Ground improvement** (e.g., gravel rafts, compaction) if required

> ?? **A full geotechnical investigation (CPTs + boreholes) is required to confirm foundation design parameters.**

---

## ?? Infrastructure

### Available Services

| Service | Available | Provider | Notes |
|---------|-----------|----------|-------|
| **Water Supply** | ? Yes | CCC | Available at street frontage |
| **Wastewater** | ? Yes | CCC | Reticulated network available |
| **Stormwater** | ? Yes | CCC | On-site attenuation required |
| **Power** | ? Yes | Orion | Network connection available |
| **Fibre** | ? Yes | Chorus | Ultra-fast broadband available |

### Stormwater Requirements

Due to site conditions, CCC requires stormwater attenuation:

- Shallow groundwater (1.0–2.0m)
- Limited soakage capacity
- Localised ponding risk

**Likely Requirements:**
- On-site detention tank
- Controlled discharge to CCC network
- Compliance with CCC Waterways, Wetlands and Drainage Guide

---

## ?? Compliance Summary

The proposed development can meet the requirements of:

| Requirement | Status |
|-------------|--------|
| Building Act 2004 | ? Achievable |
| Building Code (B1 Structure) | ? With appropriate foundation design |
| Building Code (B2 Durability) | ? Achievable |
| Building Code (E1 Surface Water) | ? With stormwater management |
| Building Code (E2 External Moisture) | ? Achievable |
| Christchurch District Plan | ? Subject to design compliance |
| CCC Engineering Standards | ? With appropriate infrastructure design |

---

## ? Next Steps

### Required Investigations
1. ? Commission full geotechnical investigation (CPTs + boreholes)
2. ? Undertake topographical survey
3. ? Prepare stormwater management plan
4. ? Confirm wastewater capacity with CCC
5. ? Confirm whether resource consent is required

### Design Phase
1. ? Proceed with preliminary structural design
2. ? Develop architectural concept (2–4 levels)
3. ? Prepare parking and access layout
4. ? Design on-site stormwater attenuation system

---

## ?? Data Sources

| Data Type | Source |
|-----------|--------|
| Property Information | LINZ / Canterbury Maps |
| Hazard Layers | Canterbury Maps / CCC GIS |
| Geotechnical Data | Canterbury Geotechnical Database (CGD) |
| Zoning | Christchurch District Plan |
| Infrastructure | CCC Asset Management |

---

## ?? Limitations

This case study is based on desktop assessment data only. No site testing or intrusive investigation has been undertaken. The findings and recommendations may change once geotechnical testing is completed.

This data is provided for demonstration and feasibility purposes only.

---

## ?? Using This Case Study

### In the Wizard

1. Go to `/SiteEvaluator/EvaluationWizard`
2. Enter "360 Barbadoes" in the address field
3. Select the address from autocomplete
4. Step through the wizard to see all data

### In the Search

1. Go to `/SiteEvaluator/Search?address=360+Barbadoes+Street`
2. View the complete evaluation results

### API Access

```http
GET /api/siteevaluator/address/autocomplete?q=360%20Barbadoes
GET /api/siteevaluator/search/address
POST body: { "address": "360 Barbadoes Street, Christchurch" }
```

---

## Related Documentation

- [API Credentials Guide](../admin/API-Credentials-Guide.md)
- [Site Evaluation Wizard Guide](Site-Evaluation-Wizard-Guide.md)
- [Engineering Report Guide](Engineering-Report-Guide.md)
