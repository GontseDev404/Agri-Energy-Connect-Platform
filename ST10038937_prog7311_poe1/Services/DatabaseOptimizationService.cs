using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using System.Diagnostics;

namespace ST10038937_prog7311_poe1.Services
{
    public interface IDatabaseOptimizationService
    {
        Task<List<T>> GetCachedDataAsync<T>(string cacheKey, Func<Task<List<T>>> dataFactory, TimeSpan? cacheDuration = null) where T : class;
        Task<T?> GetCachedSingleAsync<T>(string cacheKey, Func<Task<T?>> dataFactory, TimeSpan? cacheDuration = null) where T : class;
        Task<int> GetCachedCountAsync(string cacheKey, Func<Task<int>> countFactory, TimeSpan? cacheDuration = null);
        void InvalidateCache(string pattern);
        Task<List<T>> OptimizeQueryAsync<T>(IQueryable<T> query, bool includeRelated = false) where T : class;
        Task<T?> OptimizeSingleQueryAsync<T>(IQueryable<T> query, bool includeRelated = false) where T : class;
        Task<int> OptimizeCountQueryAsync<T>(IQueryable<T> query) where T : class;
        Task<List<T>> GetPaginatedDataAsync<T>(IQueryable<T> query, int page, int pageSize, string cacheKey = "") where T : class;
        Task<Dictionary<string, int>> GetDashboardStatsAsync();
        Task<List<T>> GetRecentDataAsync<T>(IQueryable<T> query, int count, string cacheKey = "") where T : class;
    }

    public class DatabaseOptimizationService : IDatabaseOptimizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DatabaseOptimizationService> _logger;
        private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(5);

        public DatabaseOptimizationService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<DatabaseOptimizationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<T>> GetCachedDataAsync<T>(string cacheKey, Func<Task<List<T>>> dataFactory, TimeSpan? cacheDuration = null) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (!_cache.TryGetValue(cacheKey, out List<T>? cachedData))
            {
                _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
                cachedData = await dataFactory();
                
                var duration = cacheDuration ?? _defaultCacheDuration;
                _cache.Set(cacheKey, cachedData, duration);
                
                _logger.LogInformation("Cached data for key: {CacheKey}, Duration: {Duration}, Count: {Count}", 
                    cacheKey, duration, cachedData.Count);
            }
            else
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}, Count: {Count}", cacheKey, cachedData.Count);
            }

            stopwatch.Stop();
            _logger.LogDebug("GetCachedDataAsync completed in {ElapsedMs}ms for key: {CacheKey}", 
                stopwatch.ElapsedMilliseconds, cacheKey);

            return cachedData;
        }

        public async Task<T?> GetCachedSingleAsync<T>(string cacheKey, Func<Task<T?>> dataFactory, TimeSpan? cacheDuration = null) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (!_cache.TryGetValue(cacheKey, out T? cachedData))
            {
                _logger.LogInformation("Cache miss for single item key: {CacheKey}", cacheKey);
                cachedData = await dataFactory();
                
                if (cachedData != null)
                {
                    var duration = cacheDuration ?? _defaultCacheDuration;
                    _cache.Set(cacheKey, cachedData, duration);
                    _logger.LogInformation("Cached single item for key: {CacheKey}, Duration: {Duration}", 
                        cacheKey, duration);
                }
            }
            else
            {
                _logger.LogDebug("Cache hit for single item key: {CacheKey}", cacheKey);
            }

            stopwatch.Stop();
            _logger.LogDebug("GetCachedSingleAsync completed in {ElapsedMs}ms for key: {CacheKey}", 
                stopwatch.ElapsedMilliseconds, cacheKey);

            return cachedData;
        }

        public async Task<int> GetCachedCountAsync(string cacheKey, Func<Task<int>> countFactory, TimeSpan? cacheDuration = null)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (!_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                _logger.LogInformation("Cache miss for count key: {CacheKey}", cacheKey);
                cachedCount = await countFactory();
                
                var duration = cacheDuration ?? _defaultCacheDuration;
                _cache.Set(cacheKey, cachedCount, duration);
                
                _logger.LogInformation("Cached count for key: {CacheKey}, Duration: {Duration}, Count: {Count}", 
                    cacheKey, duration, cachedCount);
            }
            else
            {
                _logger.LogDebug("Cache hit for count key: {CacheKey}, Count: {Count}", cacheKey, cachedCount);
            }

            stopwatch.Stop();
            _logger.LogDebug("GetCachedCountAsync completed in {ElapsedMs}ms for key: {CacheKey}", 
                stopwatch.ElapsedMilliseconds, cacheKey);

            return cachedCount;
        }

        public void InvalidateCache(string pattern)
        {
            // For memory cache, we can't easily invalidate by pattern
            // In a production environment, consider using distributed cache like Redis
            _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", pattern);
        }

        public async Task<List<T>> OptimizeQueryAsync<T>(IQueryable<T> query, bool includeRelated = false) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Apply optimizations
            var optimizedQuery = query.AsNoTracking();
            
            if (includeRelated)
            {
                // Include related entities if needed
                // This would need to be customized based on the entity type
            }

            var result = await optimizedQuery.ToListAsync();
            
            stopwatch.Stop();
            _logger.LogDebug("OptimizeQueryAsync completed in {ElapsedMs}ms, Count: {Count}", 
                stopwatch.ElapsedMilliseconds, result.Count);

            return result;
        }

        public async Task<T?> OptimizeSingleQueryAsync<T>(IQueryable<T> query, bool includeRelated = false) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Apply optimizations
            var optimizedQuery = query.AsNoTracking();
            
            if (includeRelated)
            {
                // Include related entities if needed
            }

            var result = await optimizedQuery.FirstOrDefaultAsync();
            
            stopwatch.Stop();
            _logger.LogDebug("OptimizeSingleQueryAsync completed in {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);

            return result;
        }

        public async Task<int> OptimizeCountQueryAsync<T>(IQueryable<T> query) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            var result = await query.CountAsync();
            
            stopwatch.Stop();
            _logger.LogDebug("OptimizeCountQueryAsync completed in {ElapsedMs}ms, Count: {Count}", 
                stopwatch.ElapsedMilliseconds, result);

            return result;
        }

        public async Task<List<T>> GetPaginatedDataAsync<T>(IQueryable<T> query, int page, int pageSize, string cacheKey = "") where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            var paginatedQuery = query
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var result = await paginatedQuery.ToListAsync();
            
            stopwatch.Stop();
            _logger.LogDebug("GetPaginatedDataAsync completed in {ElapsedMs}ms, Page: {Page}, PageSize: {PageSize}, Count: {Count}", 
                stopwatch.ElapsedMilliseconds, page, pageSize, result.Count);

            return result;
        }

        public async Task<Dictionary<string, int>> GetDashboardStatsAsync()
        {
            const string cacheKey = "dashboard_stats";
            
            return await GetCachedDataAsync(cacheKey, async () =>
            {
                var stats = new Dictionary<string, int>();
                
                // Get counts in parallel for better performance
                var tasks = new[]
                {
                    _context.Farmers.CountAsync(),
                    _context.Products.CountAsync(),
                    _context.ForumPosts.CountAsync(),
                    _context.AuditLogs.CountAsync()
                };

                var results = await Task.WhenAll(tasks);
                
                stats["Farmers"] = results[0];
                stats["Products"] = results[1];
                stats["ForumPosts"] = results[2];
                stats["AuditLogs"] = results[3];

                return new List<Dictionary<string, int>> { stats };
            }, TimeSpan.FromMinutes(2)).ContinueWith(t => t.Result.FirstOrDefault() ?? new Dictionary<string, int>());
        }

        public async Task<List<T>> GetRecentDataAsync<T>(IQueryable<T> query, int count, string cacheKey = "") where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            var recentQuery = query
                .AsNoTracking()
                .Take(count);

            var result = await recentQuery.ToListAsync();
            
            stopwatch.Stop();
            _logger.LogDebug("GetRecentDataAsync completed in {ElapsedMs}ms, Count: {Count}", 
                stopwatch.ElapsedMilliseconds, result.Count);

            return result;
        }
    }
} 