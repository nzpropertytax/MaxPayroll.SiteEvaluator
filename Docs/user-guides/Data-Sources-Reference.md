# Data Sources Reference ??

<div align="center">

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   ?? AUTHORITATIVE NEW ZEALAND DATA SOURCES                      ?
?                                                                  ?
?   Understanding where our data comes from                        ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## Overview

Site Evaluator aggregates data from multiple authoritative New Zealand sources. This document describes each source, its update frequency, and reliability.

---

## ?? Address & Property Data

### LINZ Data Service

| Attribute | Value |
|-----------|-------|
| **Provider** | Land Information New Zealand |
| **URL** | https://data.linz.govt.nz |
| **Data Types** | Addresses, parcels, titles |
| **Update Frequency** | Weekly |
| **Reliability** | ????? Official government source |

**Data Available:**
- Official street addresses
- Parcel boundaries
- Title references
- Legal descriptions
- Coordinates (WGS84/NZGD2000)

### LINZ Landonline

| Attribute | Value |
|-----------|-------|
| **Provider** | Land Information New Zealand |
| **URL** | https://www.linz.govt.nz/landonline |
| **Data Types** | Land titles, survey plans |
| **Update Frequency** | Real-time |
| **Reliability** | ????? Official register |

**Data Available:**
- Certificate of Title details
- Survey plan information
- Easements and encumbrances
- Ownership (where permitted)

---

## ??? Zoning & Planning Data

### Council GIS Services

Each council provides GIS data through their own platforms:

| Council | GIS Platform | API Type |
|---------|--------------|----------|
| Christchurch | Canterbury Maps | ArcGIS REST |
| Selwyn | Canterbury Maps | ArcGIS REST |
| Waimakariri | Canterbury Maps | ArcGIS REST |
| Auckland | Auckland GeoMaps | ArcGIS REST |
| Wellington | WCC GIS | ArcGIS REST |

**Data Available:**
- Zone boundaries
- Built form standards
- Planning overlays
- Height limits
- Setback requirements

**Update Frequency:** Varies by council (typically monthly)

**Reliability:** ???? Official but may lag district plan changes

### District Plan References

| Council | District Plan | Link |
|---------|---------------|------|
| Christchurch | Christchurch District Plan | https://districtplan.ccc.govt.nz |
| Selwyn | Selwyn District Plan | https://www.selwyn.govt.nz/property-building/district-plan |
| Waimakariri | Proposed Waimakariri District Plan | https://www.waimakariri.govt.nz |

---

## ?? Hazard Data

### Flood Hazard

| Source | Data | Update Frequency |
|--------|------|------------------|
| Council GIS | Flood zones, FMA | As flood models updated |
| Canterbury Regional Council | Regional flood data | As studies completed |

**Reliability:** ???? Based on flood modeling studies

### Liquefaction

| Source | Data | Coverage |
|--------|------|----------|
| Canterbury Council | TC1/TC2/TC3 categories | Canterbury region |
| EQC | Liquefaction susceptibility | National (general) |

**Note:** Canterbury categories are specific to post-earthquake assessments

**Reliability:** ???? Based on extensive post-earthquake investigation

### Seismic Data

| Source | Data | Update Frequency |
|--------|------|------------------|
| GNS Science | Active fault database | As research updated |
| NZS 1170.5 | Zone factors | Code cycle (5-10 years) |

**Reliability:** ????? Official national hazard model

### Contamination

| Source | Data | Update Frequency |
|--------|------|------------------|
| Council | HAIL site lists | Ongoing |
| Regional Council | LLUR database | As sites identified |

**Reliability:** ???? Official registers, may not be complete

---

## ?? Geotechnical Data

### NZ Geotechnical Database (NZGD)

| Attribute | Value |
|-----------|-------|
| **Provider** | NZ Geotechnical Society / MBIE |
| **URL** | https://www.nzgd.org.nz |
| **Data Types** | Investigation records |
| **Update Frequency** | As investigations submitted |
| **Reliability** | ???? Industry database |

**Data Available:**
- Borehole logs
- CPT results
- Laboratory test results
- Geotechnical reports (some)

**Coverage Notes:**
- Canterbury has excellent coverage (post-earthquake)
- Other regions have variable coverage
- Private investigations may not be in database

### GNS Science Geology

| Attribute | Value |
|-----------|-------|
| **Provider** | GNS Science |
| **URL** | https://www.gns.cri.nz |
| **Data Types** | Geological maps, hazard data |
| **Reliability** | ????? Scientific authority |

**Data Available:**
- Geological maps (1:250,000 national, 1:50,000 regional)
- Active fault traces
- Volcanic hazard zones
- Landslide susceptibility

---

## ?? Infrastructure Data

### Three Waters

| Council | Data Source | Reliability |
|---------|-------------|-------------|
| Christchurch | CCC GIS / Asset Management | ???? |
| Selwyn | SDC Asset Management | ???? |
| Waimakariri | WDC Asset Management | ???? |

**Data Available:**
- Water main locations and sizes
- Sewer main locations
- Stormwater network
- Connection availability (indicative)

**Note:** Actual connection capacity requires council confirmation

### Power & Communications

| Provider | Data Available |
|----------|----------------|
| Electricity Lines Companies | Network areas, general capacity |
| Chorus | Fibre availability |
| Gas distributors | Network coverage |

**Reliability:** ??? General availability only, not capacity confirmation

---

## ??? Climate Data

### NIWA (National Institute of Water & Atmospheric Research)

| Attribute | Value |
|-----------|-------|
| **Provider** | NIWA |
| **URL** | https://niwa.co.nz |
| **Data Types** | Climate statistics |
| **Reliability** | ????? Scientific authority |

**Data Available:**
- Rainfall statistics (annual, design intensities)
- Wind data
- Temperature statistics
- Climate normals

### Building Code Climate Data

| Source | Data | Use |
|--------|------|-----|
| NZS 3604 | Wind zones | Timber-framed construction |
| NZS 4223 | Snow loading zones | Structural design |
| NIWA HIRDs | Rainfall intensity | Stormwater design |

---

## ?? Data Quality Summary

| Data Type | Primary Source | Reliability | Verification Recommended |
|-----------|----------------|-------------|-------------------------|
| Address | LINZ | ????? | No |
| Zoning | Council GIS | ???? | For consent applications |
| Flooding | Council/Regional | ???? | For building consent |
| Liquefaction | Council | ???? | With geotech investigation |
| Seismic | GNS/Standards | ????? | No |
| Contamination | Council/Regional | ???? | For development sites |
| Geotech | NZGD | ???? | Always site-specific |
| Infrastructure | Council | ???? | For connection confirmation |
| Climate | NIWA | ????? | No |

---

## ?? Important Notes

### Data Currency

- All data has retrieval timestamps in reports
- Some sources update infrequently
- Recent changes may not be reflected
- Always verify critical data for formal applications

### Data Completeness

- Not all councils have complete GIS coverage
- NZGD coverage varies by region
- Private data may not be in public databases
- Absence of data ? absence of constraint

### Professional Use

This data is provided to support professional assessments but:
- Does not replace professional judgment
- May require verification from primary sources
- Should be supplemented with site-specific investigation
- Cannot capture all site-specific conditions

---

## ?? Related Documentation

- [Site Evaluation Wizard Guide](Site-Evaluation-Wizard-Guide.md)
- [Engineering Report Guide](Engineering-Report-Guide.md)
- [API Implementation Guides](../API-Implementation-Guides/README.md)
