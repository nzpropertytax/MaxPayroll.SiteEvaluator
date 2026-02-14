using LiteDB;
using MaxPayroll.SiteEvaluator.Models;

namespace MaxPayroll.SiteEvaluator.Services;

/// <summary>
/// Simple LiteDB repository for SiteEvaluator data.
/// Self-contained - no dependency on Website project.
/// </summary>
public interface ISiteEvaluatorRepository
{
    Task<T?> GetByIdAsync<T>(string id) where T : class;
    Task<IEnumerable<T>> GetAllAsync<T>() where T : class;
    Task<IEnumerable<T>> FindAsync<T>(Func<T, bool> predicate) where T : class;
    Task InsertAsync<T>(T entity) where T : class;
    Task UpdateAsync<T>(T entity) where T : class;
    Task<bool> DeleteAsync(string id);
    
    // Report storage
    Task StoreReportAsync(string reportId, byte[] content);
    Task<byte[]?> GetReportAsync(string reportId);
}

/// <summary>
/// LiteDB implementation of the SiteEvaluator repository.
/// </summary>
public class SiteEvaluatorRepository : ISiteEvaluatorRepository, IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly ILogger<SiteEvaluatorRepository> _logger;
    
    // Collection names
    private const string EvaluationsCollection = "site_evaluations";
    private const string SubscriptionsCollection = "site_evaluator_subscriptions";
    private const string JobsCollection = "evaluation_jobs";
    private const string LocationsCollection = "property_locations";
    private const string ReportsCollection = "report_files";

    public SiteEvaluatorRepository(IConfiguration configuration, ILogger<SiteEvaluatorRepository> logger)
    {
        _logger = logger;
        
        // Use a separate database file for SiteEvaluator
        var dataPath = configuration["SiteEvaluator:DataPath"] ?? "Data";
        var dbPath = Path.Combine(dataPath, "site-evaluator.db");
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        _database = new LiteDatabase(dbPath);
        _logger.LogInformation("SiteEvaluator database initialized at {Path}", dbPath);
        
        // Ensure indexes
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        // Evaluations
        var evaluations = _database.GetCollection<SiteEvaluation>(EvaluationsCollection);
        evaluations.EnsureIndex(x => x.UserId);
        evaluations.EnsureIndex(x => x.CreatedDate);
        evaluations.EnsureIndex(x => x.Location.Address);
        
        // Jobs
        var jobs = _database.GetCollection<EvaluationJob>(JobsCollection);
        jobs.EnsureIndex(x => x.JobReference);
        jobs.EnsureIndex(x => x.LocationId);
        jobs.EnsureIndex(x => x.CreatedByUserId);
        jobs.EnsureIndex(x => x.CustomerName);
        jobs.EnsureIndex(x => x.Status);
        jobs.EnsureIndex(x => x.CreatedDate);
        
        // Locations
        var locations = _database.GetCollection<PropertyLocation>(LocationsCollection);
        locations.EnsureIndex(x => x.Address);
        locations.EnsureIndex(x => x.TitleReference);
        locations.EnsureIndex(x => x.Latitude);
        locations.EnsureIndex(x => x.Longitude);
    }

    public Task<T?> GetByIdAsync<T>(string id) where T : class
    {
        var collectionName = GetCollectionName<T>();
        var collection = _database.GetCollection<T>(collectionName);
        var result = collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<T>> GetAllAsync<T>() where T : class
    {
        var collectionName = GetCollectionName<T>();
        var collection = _database.GetCollection<T>(collectionName);
        var results = collection.FindAll().ToList();
        return Task.FromResult<IEnumerable<T>>(results);
    }

    public Task<IEnumerable<T>> FindAsync<T>(Func<T, bool> predicate) where T : class
    {
        var collectionName = GetCollectionName<T>();
        var collection = _database.GetCollection<T>(collectionName);
        
        try
        {
            var results = collection.FindAll()
                .Where(item =>
                {
                    try
                    {
                        return predicate(item);
                    }
                    catch
                    {
                        // Predicate failed (e.g., null reference) - exclude this item
                        return false;
                    }
                })
                .ToList();
            return Task.FromResult<IEnumerable<T>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FindAsync for collection {Collection}", collectionName);
            return Task.FromResult<IEnumerable<T>>(new List<T>());
        }
    }

    public Task InsertAsync<T>(T entity) where T : class
    {
        var collectionName = GetCollectionName<T>();
        var collection = _database.GetCollection<T>(collectionName);
        collection.Insert(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync<T>(T entity) where T : class
    {
        var collectionName = GetCollectionName<T>();
        var collection = _database.GetCollection<T>(collectionName);
        collection.Update(entity);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id)
    {
        // Try evaluations first, then jobs
        var evaluations = _database.GetCollection<SiteEvaluation>(EvaluationsCollection);
        if (evaluations.Delete(id))
            return Task.FromResult(true);
        
        var jobs = _database.GetCollection<EvaluationJob>(JobsCollection);
        if (jobs.Delete(id))
            return Task.FromResult(true);
        
        var locations = _database.GetCollection<PropertyLocation>(LocationsCollection);
        return Task.FromResult(locations.Delete(id));
    }

    public Task StoreReportAsync(string reportId, byte[] content)
    {
        var collection = _database.GetCollection<ReportFile>(ReportsCollection);
        var report = new ReportFile
        {
            Id = reportId,
            Content = content,
            CreatedDate = DateTime.UtcNow
        };
        collection.Upsert(report);
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetReportAsync(string reportId)
    {
        var collection = _database.GetCollection<ReportFile>(ReportsCollection);
        var report = collection.FindById(reportId);
        return Task.FromResult(report?.Content);
    }

    private static string GetCollectionName<T>()
    {
        return typeof(T).Name switch
        {
            nameof(SiteEvaluation) => EvaluationsCollection,
            nameof(SiteEvaluatorSubscription) => SubscriptionsCollection,
            nameof(EvaluationJob) => JobsCollection,
            nameof(PropertyLocation) => LocationsCollection,
            _ => typeof(T).Name.ToLowerInvariant()
        };
    }

    public void Dispose()
    {
        _database?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal class for storing report binary content.
/// </summary>
internal class ReportFile
{
    public string Id { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public DateTime CreatedDate { get; set; }
}
