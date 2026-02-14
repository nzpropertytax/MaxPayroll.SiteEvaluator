# LINZ Landonline API Implementation Guide

**Service**: LINZ Landonline - Property Title Data  
**Status**: ? Implemented (with fallback data)  
**Implementation File**: `Services/Integration/LinzDataService.cs`

---

## ?? Overview

LINZ Landonline is New Zealand's authoritative source for property title data, including:
- Certificate of Title information
- Ownership details (current and historical)
- Legal descriptions
- Easements, covenants, and encumbrances
- Survey plans

---

## ? Current Implementation

The LinzDataService now provides:

| Feature | Without Subscription | With Subscription |
|---------|---------------------|-------------------|
| Title lookup | ? Basic info (estimated) | ? Full details |
| Ownership | ? Not available | ? Full ownership |
| Encumbrances | ? Not available | ? Full list |
| Title search | ? Not available | ? Search by address |
| History | ? Not available | ? Transfer history |

### API Endpoints

```http
GET /api/siteevaluator/titles/{titleReference}
GET /api/siteevaluator/titles/search?address={address}
GET /api/siteevaluator/titles/{titleReference}/history
```

### Example: Get Title Data

**Request**:
```http
GET /api/siteevaluator/titles/CB45A/123
```

**Response (without Landonline subscription)**:
```json
{
  "titleReference": "CB45A/123",
  "titleType": "Freehold",
  "titleStatus": "Live (Estimated)",
  "legalDescription": "Title CB45A/123 - full details require Landonline subscription",
  "owners": [],
  "easements": [],
  "covenants": [],
  "otherEncumbrances": [],
  "source": {
    "sourceName": "LINZ (Estimated)",
    "sourceUrl": "https://www.linz.govt.nz/",
    "notes": "Full title data requires Landonline subscription (~$500/year)."
  }
}
```

**Response (with Landonline subscription)**:
```json
{
  "titleReference": "CB45A/123",
  "titleType": "Freehold",
  "titleStatus": "Live",
  "titleDate": "1985-06-15",
  "legalDescription": "Lot 1 DP 12345",
  "lotNumber": "1",
  "dpNumber": "DP 12345",
  "areaSquareMeters": 456.0,
  "areaHectares": 0.0456,
  "owners": [
    {
      "name": "John Smith",
      "share": "1/1"
    }
  ],
  "easements": [
    {
      "type": "Right of Way",
      "purpose": "Pedestrian and vehicular access",
      "inFavourOf": "Lot 2 DP 12345",
      "documentReference": "E67890"
    }
  ],
  "covenants": [
    {
      "type": "Building Covenant",
      "description": "No building within 3m of boundary",
      "documentReference": "C11111"
    }
  ],
  "otherEncumbrances": [
    {
      "type": "Mortgage",
      "description": "Mortgagee: ANZ Bank",
      "documentReference": "M12345"
    }
  ],
  "source": {
    "sourceName": "LINZ Landonline",
    "sourceUrl": "https://www.landonline.govt.nz/"
  }
}
```
