# Service Implementation Guide ??

<div align="center">

```
????????????????????????????????????????????????????????????????????
?                                                                  ?
?   ?? ADDING NEW DATA SERVICES                                    ?
?                                                                  ?
?   How to extend Site Evaluator with new data sources             ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

</div>

---

## ?? Overview

This guide explains how to add new data services to the Site Evaluator. Follow the established patterns for consistency and maintainability.

---

## ?? File Structure

When adding a new service:

```
MaxPayroll.SiteEvaluator/
??? Services/
?   ??? Integration/
?       ??? IntegrationInterfaces.cs    # Add interface here
?       ??? YourNewService.cs           # Implementation
??? Models/
?   ??? YourNewData.cs                  # Data models
??? Docs/
    ??? API-Implementation-Guides/
        ??? Your-New-API.md             # Documentation
```

---

## ??? Implementation Pattern

### Step 1: Define the Interface

Add your interface to `IntegrationInterfaces.cs`:

```csharp
/// <summary>
/// Interface for Your New Data Service.
/// </summary>
public interface IYourNewDataService
{
    /// <summary>
    /// Get data for a location.
    /// </summary>
    Task<YourNewData?> GetDataAsync(
        double lat, double lon, 
        CancellationToken ct = default);
}
```

### Step 2: Create Data Models

Create models in `Models/YourNewData.cs`:

```csharp
namespace MaxPayroll.SiteEvaluator.Models;

/// <summary>
/// Data from Your New Service.
/// </summary>
public class YourNewData
{
    public string SomeProperty { get; set; } = string.Empty;
    public double? SomeValue { get; set; }
    public List<string> SomeList { get; set; } = [];
    
    /// <summary>
    /// Data source information.
    /// </summary>
    public DataSource? Source { get; set; }
}
```

### Step 3: Implement the Service

Create implementation in `Services/Integration/YourNewService.cs`:

```csharp
namespace MaxPayroll.SiteEvaluator.Services.Integration;

public class YourNewDataService : IYourNewDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YourNewDataService> _logger;

    public YourNewDataService(
        HttpClient httpClient, 
        ILogger<YourNewDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<YourNewData?> GetDataAsync(
        double lat, double lon, 
        CancellationToken ct = default)
    {
        try
        {
            // Try API first
            var data = await TryApiAsync(lat, lon, ct);
            if (data != null)
                return data;
            
            // Fall back to static data
            return GetStaticData(lat, lon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data for {Lat}, {Lon}", lat, lon);
            return GetStaticData(lat, lon);
        }
    }

    private async Task<YourNewData?> TryApiAsync(
        double lat, double lon, 
        CancellationToken ct)
    {
        try
        {
            var url = $"https://api.example.com/data?lat={lat}&lon={lon}";
            var response = await _httpClient.GetAsync(url, ct);
            
            if (!response.IsSuccessStatusCode)
                return null;
            
            var apiResponse = await response.Content
                .ReadFromJsonAsync<ApiResponse>(ct);
            
            return MapToModel(apiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API call failed");
            return null;
        }
    }

    private YourNewData GetStaticData(double lat, double lon)
    {
        // Provide reasonable fallback data
        return new YourNewData
        {
            SomeProperty = "Estimated",
            Source = new DataSource
            {
                SourceName = "Static fallback data",
                RetrievedDate = DateTime.UtcNow,
                Notes = "API unavailable, using estimated values"
            }
        };
    }
}
```

### Step 4: Register the Service

Add to `Configuration/SiteEvaluatorServiceExtensions.cs`:

```csharp
public static IServiceCollection AddSiteEvaluatorServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing registrations ...
    
    // Your new service
    services.AddHttpClient<IYourNewDataService, YourNewDataService>(client =>
    {
        client.BaseAddress = new Uri(
            configuration["SiteEvaluator:YourNew:BaseUrl"] 
            ?? "https://api.example.com");
        
        var apiKey = configuration["SiteEvaluator:YourNew:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"key {apiKey}");
        }
    });
    
    return services;
}
```

### Step 5: Integrate with Evaluation

Update `SiteSearchService` to use your new service:

```csharp
public async Task<SiteEvaluation> EvaluateSiteAsync(
    double lat, double lon, 
    CancellationToken ct)
{
    var evaluation = new SiteEvaluation();
    
    // ... existing calls ...
    
    // Add your new data
    evaluation.YourNewData = await _yourNewService.GetDataAsync(lat, lon, ct);
    
    return evaluation;
}
```

---

## ?? Best Practices

### Error Handling

Always provide fallback data:

```csharp
public async Task<Data?> GetDataAsync(...)
{
    try
    {
        var apiData = await TryApiAsync(...);
        if (apiData != null) return apiData;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "API failed, using fallback");
    }
    
    return GetStaticFallback(...);  // Never return null if avoidable
}
```

### Data Source Tracking

Always include source information:

```csharp
return new YourData
{
    // ... data ...
    Source = new DataSource
    {
        SourceName = "Your API Name",
        SourceUrl = "https://api.example.com",
        DataDate = response.LastUpdated,
        RetrievedDate = DateTime.UtcNow,
        Notes = "Any relevant notes about data quality"
    }
};
```

### Caching

Consider caching for expensive API calls:

```csharp
private readonly IMemoryCache _cache;

public async Task<Data?> GetDataAsync(double lat, double lon, ...)
{
    var cacheKey = $"yourdata_{lat:F4}_{lon:F4}";
    
    if (_cache.TryGetValue(cacheKey, out Data? cached))
        return cached;
    
    var data = await FetchDataAsync(lat, lon, ...);
    
    _cache.Set(cacheKey, data, TimeSpan.FromHours(24));
    
    return data;
}
```

### Regional Lookup

Use consistent region detection:

```csharp
private string GetRegion(double lat, double lon)
{
    // Use the standard region mapping
    if (lat > -37.5) return "Auckland";
    if (lat > -38.5 && lon < 176) return "Hamilton";
    // ... etc
}
```

---

## ?? Testing

### Unit Test Pattern

```csharp
public class YourNewServiceTests
{
    [Fact]
    public async Task GetDataAsync_ValidLocation_ReturnsData()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.When("*").Respond(
            "application/json", 
            "{\"value\": 42}");
        
        var httpClient = mockHandler.ToHttpClient();
        var service = new YourNewDataService(
            httpClient, 
            Mock.Of<ILogger<YourNewDataService>>());
        
        // Act
        var result = await service.GetDataAsync(-43.53, 172.63);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.SomeValue);
    }

    [Fact]
    public async Task GetDataAsync_ApiError_ReturnsFallback()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.When("*").Respond(HttpStatusCode.ServiceUnavailable);
        
        var httpClient = mockHandler.ToHttpClient();
        var service = new YourNewDataService(
            httpClient,
            Mock.Of<ILogger<YourNewDataService>>());
        
        // Act
        var result = await service.GetDataAsync(-43.53, 172.63);
        
        // Assert
        Assert.NotNull(result);  // Should return fallback, not null
    }
}
```

### Integration Test Pattern

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task GetDataAsync_RealApi_ReturnsValidData()
{
    // Only run if API key configured
    var apiKey = Environment.GetEnvironmentVariable("YOUR_API_KEY");
    if (string.IsNullOrEmpty(apiKey)) return;
    
    // Arrange
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"key {apiKey}");
    
    var service = new YourNewDataService(
        httpClient,
        Mock.Of<ILogger<YourNewDataService>>());
    
    // Act - Use known test location
    var result = await service.GetDataAsync(-43.53, 172.63);
    
    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Source);
}
```

---

## ?? Documentation

Create API guide in `Docs/API-Implementation-Guides/Your-New-API.md`:

```markdown
# Your New API Implementation Guide

**Service**: Your Service Name  
**Status**: ? Implemented (80%)  
**Implementation File**: `Services/Integration/YourNewService.cs`

---

## ?? Overview

Description of what this API provides...

---

## ?? Authentication

How to get API keys, configure authentication...

---

## ?? API Endpoints

### Endpoint 1

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| lat | double | Yes | Latitude |
| lon | double | Yes | Longitude |

**Example Request**:
```http
GET https://api.example.com/data?lat=-43.53&lon=172.63
```

---

## ? Implementation Checklist

- [x] Interface definition
- [x] Basic implementation
- [ ] Full API integration
- [ ] Caching
- [ ] Unit tests
- [ ] Integration tests
```

---

## ?? Related Documentation

- [API Implementation Overview](../API-Implementation-Guides/README.md)
- [AI Learning Model Guide](AI-Learning-Model-Guide.md)
- [User Guides](../user-guides/README.md)
