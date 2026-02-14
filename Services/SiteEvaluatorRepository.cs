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
}

/// <summary>
/// LiteDB implementation of the SiteEvaluator repository.
/// </summary>
public class SiteEvaluatorRepository : ISiteEvaluatorRepository, IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly ILogger<SiteEvaluatorRepository> _logger;
    private const string EvaluationsCollection = "site_evaluations";
    private const string SubscriptionsCollection = "site_evaluator_subscriptions";

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
        var evaluations = _database.GetCollection<SiteEvaluation>(EvaluationsCollection);
        evaluations.EnsureIndex(x => x.UserId);
        evaluations.EnsureIndex(x => x.CreatedDate);
        evaluations.EnsureIndex(x => x.Location.Address);
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
        var results = collection.FindAll().Where(predicate).ToList();
        return Task.FromResult<IEnumerable<T>>(results);
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
        // Try evaluations first
        var evaluations = _database.GetCollection<SiteEvaluation>(EvaluationsCollection);
        var deleted = evaluations.Delete(id);
        return Task.FromResult(deleted);
    }

    private static string GetCollectionName<T>()
    {
        return typeof(T).Name switch
        {
            nameof(SiteEvaluation) => EvaluationsCollection,
            nameof(SiteEvaluatorSubscription) => SubscriptionsCollection,
            _ => typeof(T).Name.ToLowerInvariant()
        };
    }

    public void Dispose()
    {
        _database?.Dispose();
        GC.SuppressFinalize(this);
    }
}
