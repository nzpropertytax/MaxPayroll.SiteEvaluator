using MaxPayroll.SiteEvaluator.Services;
using MaxPayroll.SiteEvaluator.Services.Integration;

namespace MaxPayroll.SiteEvaluator;

/// <summary>
/// Extension methods to add Site Evaluator services to the MaxPayroll.Website platform.
/// </summary>
public static class SiteEvaluatorServiceExtensions
{
    /// <summary>
    /// Add Site Evaluator services to the application.
    /// Call this from the main Program.cs: builder.Services.AddSiteEvaluator(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddSiteEvaluator(this IServiceCollection services, IConfiguration configuration)
    {
        // Application services
        services.AddScoped<ISiteSearchService, SiteSearchService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // External API integration services
        services.AddHttpClient<ILinzDataService, LinzDataService>(client =>
        {
            client.BaseAddress = new Uri(configuration["SiteEvaluator:Linz:BaseUrl"] ?? "https://data.linz.govt.nz");
        });

        services.AddHttpClient<INzgdDataService, NzgdDataService>(client =>
        {
            client.BaseAddress = new Uri(configuration["SiteEvaluator:Nzgd:BaseUrl"] ?? "https://www.nzgd.org.nz");
        });

        services.AddHttpClient<IGnsDataService, GnsDataService>(client =>
        {
            client.BaseAddress = new Uri(configuration["SiteEvaluator:Gns:BaseUrl"] ?? "https://api.gns.cri.nz");
        });

        services.AddHttpClient<INiwaDataService, NiwaDataService>(client =>
        {
            client.BaseAddress = new Uri(configuration["SiteEvaluator:Niwa:BaseUrl"] ?? "https://cliflo.niwa.co.nz");
        });

        // Council data services
        services.AddHttpClient<ChristchurchCouncilService>();
        services.AddHttpClient<AucklandCouncilService>();
        services.AddHttpClient<WellingtonCouncilService>();

        // Register council services as a collection
        services.AddScoped<ICouncilDataService, ChristchurchCouncilService>();
        services.AddScoped<ICouncilDataService, AucklandCouncilService>();
        services.AddScoped<ICouncilDataService, WellingtonCouncilService>();

        return services;
    }

    /// <summary>
    /// Map Site Evaluator endpoints.
    /// Call this from the main Program.cs: app.MapSiteEvaluatorEndpoints();
    /// </summary>
    public static IEndpointRouteBuilder MapSiteEvaluatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/siteevaluator").MapSiteEvaluatorApi();
        return app;
    }
}

