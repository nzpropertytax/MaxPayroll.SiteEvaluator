using MaxPayroll.SiteEvaluator.Services;
using MaxPayroll.SiteEvaluator.Services.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaxPayroll.SiteEvaluator.Configuration;

/// <summary>
/// Extension methods for registering SiteEvaluator services.
/// Call AddSiteEvaluatorServices() in the host application's Program.cs.
/// </summary>
public static class SiteEvaluatorServiceExtensions
{
    /// <summary>
    /// Adds SiteEvaluator services to the DI container.
    /// </summary>
    public static IServiceCollection AddSiteEvaluatorServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repository (singleton - holds LiteDB connection)
        services.AddSingleton<ISiteEvaluatorRepository, SiteEvaluatorRepository>();

        // Core services
        services.AddScoped<ISiteSearchService, SiteSearchService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // Integration services - HttpClient factory pattern
        services.AddHttpClient<ILinzDataService, LinzDataService>(client =>
        {
            client.BaseAddress = new Uri(configuration["SiteEvaluator:Linz:BaseUrl"] ?? "https://data.linz.govt.nz");
            var apiKey = configuration["SiteEvaluator:Linz:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"key {apiKey}");
            }
        });

        services.AddHttpClient<INzgdDataService, NzgdDataService>(client =>
        {
            client.BaseAddress = new Uri(configuration["SiteEvaluator:Nzgd:BaseUrl"] ?? "https://www.nzgd.org.nz");
        });

        // Council services (multiple implementations)
        services.AddHttpClient<ICouncilDataService, ChristchurchCouncilService>();
        // Add more councils as they're implemented:
        // services.AddHttpClient<ICouncilDataService, AucklandCouncilService>();
        // services.AddHttpClient<ICouncilDataService, WellingtonCouncilService>();

        services.AddHttpClient<IGnsDataService, GnsDataService>();
        services.AddHttpClient<INiwaDataService, NiwaDataService>();

        // Options
        services.Configure<SiteEvaluatorOptions>(configuration.GetSection("SiteEvaluator"));

        return services;
    }

    /// <summary>
    /// Maps SiteEvaluator API endpoints.
    /// Call this in the host application's Program.cs after MapRazorPages().
    /// </summary>
    public static IEndpointRouteBuilder MapSiteEvaluatorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/siteevaluator")
            .WithTags("SiteEvaluator");
        
        group.MapSiteEvaluatorApi();
        
        return endpoints;
    }
}

/// <summary>
/// Configuration options for SiteEvaluator.
/// </summary>
public class SiteEvaluatorOptions
{
    public LinzOptions Linz { get; set; } = new();
    public NzgdOptions Nzgd { get; set; } = new();
    public StripeOptions Stripe { get; set; } = new();
}

public class LinzOptions
{
    public string BaseUrl { get; set; } = "https://data.linz.govt.nz";
    public string ApiKey { get; set; } = string.Empty;
}

public class NzgdOptions
{
    public string BaseUrl { get; set; } = "https://www.nzgd.org.nz";
    public string ApiKey { get; set; } = string.Empty;
}

public class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
