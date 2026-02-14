using MaxPayroll.SiteEvaluator.Models;
using MaxPayroll.SiteEvaluator.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaxPayroll.SiteEvaluator;

/// <summary>
/// REST API endpoints for Site Evaluator.
/// </summary>
public static class SiteEvaluatorEndpoints
{
    public static RouteGroupBuilder MapSiteEvaluatorApi(this RouteGroupBuilder group)
    {
        // Search endpoints
        group.MapPost("/search/address", SearchByAddress)
            .WithName("SearchByAddress")
            .WithDescription("Search for a site by address");
            
        group.MapPost("/search/title", SearchByTitle)
            .WithName("SearchByTitle")
            .WithDescription("Search for a site by title reference");
            
        group.MapPost("/search/coordinates", SearchByCoordinates)
            .WithName("SearchByCoordinates")
            .WithDescription("Search for a site by coordinates");

        // Evaluation endpoints
        group.MapGet("/evaluations/{id}", GetEvaluation)
            .WithName("GetEvaluation")
            .WithDescription("Get a site evaluation by ID");
            
        group.MapGet("/evaluations", GetUserEvaluations)
            .WithName("GetUserEvaluations")
            .WithDescription("Get all evaluations for the current user")
            .RequireAuthorization();
            
        group.MapDelete("/evaluations/{id}", DeleteEvaluation)
            .WithName("DeleteEvaluation")
            .WithDescription("Delete a site evaluation")
            .RequireAuthorization();
            
        group.MapPost("/evaluations/{id}/refresh", RefreshEvaluation)
            .WithName("RefreshEvaluation")
            .WithDescription("Refresh data for an evaluation")
            .RequireAuthorization();

        // Report endpoints
        group.MapGet("/reports/{evaluationId}/full", GenerateFullReport)
            .WithName("GenerateFullReport")
            .WithDescription("Generate a full PDF report")
            .RequireAuthorization();
            
        group.MapGet("/reports/{evaluationId}/summary", GenerateSummaryReport)
            .WithName("GenerateSummaryReport")
            .WithDescription("Generate a summary PDF report");
            
        group.MapGet("/reports/{evaluationId}/geotech", GenerateGeotechBrief)
            .WithName("GenerateGeotechBrief")
            .WithDescription("Generate a geotechnical brief PDF");

        return group;
    }

    // === Search Handlers ===

    private static async Task<IResult> SearchByAddress(
        [FromBody] AddressSearchRequest request,
        ISiteSearchService searchService)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
            return Results.BadRequest("Address is required");

        var evaluation = await searchService.SearchByAddressAsync(request.Address);
        return Results.Ok(evaluation);
    }

    private static async Task<IResult> SearchByTitle(
        [FromBody] TitleSearchRequest request,
        ISiteSearchService searchService)
    {
        if (string.IsNullOrWhiteSpace(request.TitleReference))
            return Results.BadRequest("Title reference is required");

        var evaluation = await searchService.SearchByTitleAsync(request.TitleReference);
        return Results.Ok(evaluation);
    }

    private static async Task<IResult> SearchByCoordinates(
        [FromBody] CoordinateSearchRequest request,
        ISiteSearchService searchService)
    {
        if (request.Latitude == 0 || request.Longitude == 0)
            return Results.BadRequest("Valid coordinates are required");

        var evaluation = await searchService.SearchByCoordinatesAsync(request.Latitude, request.Longitude);
        return Results.Ok(evaluation);
    }

    // === Evaluation Handlers ===

    private static async Task<IResult> GetEvaluation(
        string id,
        ISiteSearchService searchService)
    {
        var evaluation = await searchService.GetEvaluationAsync(id);
        
        if (evaluation == null)
            return Results.NotFound();

        return Results.Ok(evaluation);
    }

    private static async Task<IResult> GetUserEvaluations(
        HttpContext httpContext,
        ISiteSearchService searchService)
    {
        var userId = httpContext.User.Identity?.Name ?? "";
        var evaluations = await searchService.GetUserEvaluationsAsync(userId);
        return Results.Ok(evaluations);
    }

    private static async Task<IResult> DeleteEvaluation(
        string id,
        ISiteSearchService searchService)
    {
        var success = await searchService.DeleteEvaluationAsync(id);
        
        if (!success)
            return Results.NotFound();

        return Results.NoContent();
    }

    private static async Task<IResult> RefreshEvaluation(
        string id,
        [FromBody] RefreshRequest request,
        ISiteSearchService searchService)
    {
        try
        {
            var evaluation = await searchService.RefreshDataAsync(id, request.Sections);
            return Results.Ok(evaluation);
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
    }

    // === Report Handlers ===

    private static async Task<IResult> GenerateFullReport(
        string evaluationId,
        ISiteSearchService searchService,
        IReportService reportService)
    {
        var evaluation = await searchService.GetEvaluationAsync(evaluationId);
        
        if (evaluation == null)
            return Results.NotFound();

        var pdfBytes = await reportService.GenerateFullReportAsync(evaluation, new ReportOptions());
        
        return Results.File(
            pdfBytes,
            "application/pdf",
            $"SiteEvaluation_{evaluation.Location.Address.Replace(" ", "_")}.pdf");
    }

    private static async Task<IResult> GenerateSummaryReport(
        string evaluationId,
        ISiteSearchService searchService,
        IReportService reportService)
    {
        var evaluation = await searchService.GetEvaluationAsync(evaluationId);
        
        if (evaluation == null)
            return Results.NotFound();

        var pdfBytes = await reportService.GenerateSummaryReportAsync(evaluation);
        
        return Results.File(
            pdfBytes,
            "application/pdf",
            $"SiteSummary_{evaluation.Location.Address.Replace(" ", "_")}.pdf");
    }

    private static async Task<IResult> GenerateGeotechBrief(
        string evaluationId,
        ISiteSearchService searchService,
        IReportService reportService)
    {
        var evaluation = await searchService.GetEvaluationAsync(evaluationId);
        
        if (evaluation == null)
            return Results.NotFound();

        var pdfBytes = await reportService.GenerateGeotechBriefAsync(evaluation);
        
        return Results.File(
            pdfBytes,
            "application/pdf",
            $"GeotechBrief_{evaluation.Location.Address.Replace(" ", "_")}.pdf");
    }
}

// Request DTOs
public record AddressSearchRequest(string Address);
public record TitleSearchRequest(string TitleReference);
public record CoordinateSearchRequest(double Latitude, double Longitude);
public record RefreshRequest(List<string> Sections);
