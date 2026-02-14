# Site Evaluator - User Guides

Documentation for users of the Site Evaluator system.

---

## ?? Documentation in This Folder

| Document | Description |
|----------|-------------|
| [Site Evaluation Wizard Guide](Site-Evaluation-Wizard-Guide.md) | Step-by-step guide to using the evaluation wizard |
| [Case Study: 360 Barbadoes Street](Case-Study-360-Barbadoes.md) | Primary test case - multi-unit residential development |
| [Data Sources Reference](Data-Sources-Reference.md) | Where the data comes from |
| [Engineering Report Guide](Engineering-Report-Guide.md) | Understanding the generated reports |
| [Sample Engineering Report](Sample-Engineering-Report.md) | Example report output |

---

## ?? Quick Start

### Using the Wizard

1. Navigate to `/SiteEvaluator/EvaluationWizard`
2. Enter a New Zealand address
3. Select the matching property
4. Step through each data section
5. Generate your report

### Quick Search

1. Navigate to `/SiteEvaluator/Search`
2. Enter an address directly
3. View instant results

---

## ?? Test Data

When API keys are not configured, the system uses mock data for testing:

### Primary Case Study
**360 Barbadoes Street, Christchurch** - A multi-unit residential development site with:
- Residential Medium Density (RMD) zoning
- TC2 liquefaction category
- Full infrastructure available
- Comprehensive engineering assessment data

Type "360 Barbadoes" or "Barbadoes" in the address field to test.

### Other Test Addresses
- 90 Armagh Street, Christchurch
- 1 Worcester Boulevard, Christchurch
- 1 Queen Street, Auckland
- 1 Willis Street, Wellington

---

## ?? Understanding Results

### Hazard Categories

| Category | Meaning |
|----------|---------|
| ?? TC1 | Low liquefaction risk |
| ?? TC2 | Moderate liquefaction risk - engineering required |
| ?? TC3 | High liquefaction risk - specific foundation design required |

### Data Completeness

The completeness percentage shows how much data was retrieved:
- **80-100%** - Comprehensive data available
- **50-79%** - Partial data, some gaps
- **<50%** - Limited data, investigation recommended

---

## Related Documentation

- [API Credentials Guide](../admin/API-Credentials-Guide.md) - Setting up external API connections
- [Service Implementation Guide](../development/Service-Implementation-Guide.md) - Technical details
