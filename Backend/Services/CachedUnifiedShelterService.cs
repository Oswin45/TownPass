using Backend.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services
{
    /// <summary>
    /// 快取版的統一避難所服務
    /// 使用 SQLite 資料庫快取外部 API 的資料，減少 API 呼叫次數
    /// </summary>
    public class CachedUnifiedShelterService
    {
        private readonly UnifiedShelterService _unifiedShelterService;
        private readonly ShelterRepository _repository;
        private readonly ILogger<CachedUnifiedShelterService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        // 記憶體快取的時間（秒）
        private const string MEMORY_CACHE_KEY = "SheltersInMemory";
        private readonly TimeSpan _memoryCacheDuration;

        public CachedUnifiedShelterService(
            UnifiedShelterService unifiedShelterService,
            ShelterRepository repository,
            ILogger<CachedUnifiedShelterService> logger,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _unifiedShelterService = unifiedShelterService;
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
            _memoryCache = memoryCache;

            // 從設定檔讀取記憶體快取時間，預設 5 分鐘
            var cacheMinutes = configuration.GetValue<int>("ShelterCache:MemoryCacheMinutes", 5);
            _memoryCacheDuration = TimeSpan.FromMinutes(cacheMinutes);
        }

        /// <summary>
        /// 獲取所有避難所（優先使用快取）
        /// </summary>
        public async Task<List<Shelter>> GetAllSheltersAsync()
        {
            // 1. 先檢查記憶體快取
            if (_memoryCache.TryGetValue(MEMORY_CACHE_KEY, out List<Shelter>? cachedShelters) && cachedShelters != null)
            {
                _logger.LogDebug("從記憶體快取獲取避難所資料");
                return cachedShelters;
            }

            // 2. 檢查資料庫快取
            if (await _repository.HasCacheAsync())
            {
                _logger.LogInformation("從資料庫快取獲取避難所資料");
                var shelters = await _repository.GetAllSheltersAsync();
                shelters.ForEach(s => s.CurrentOccupancy = Random.Shared.Next(0, s.Capacity));
                // 存入記憶體快取
                _memoryCache.Set(MEMORY_CACHE_KEY, shelters, _memoryCacheDuration);

                return shelters;
            }

            // 3. 無快取，從外部 API 獲取並快取
            _logger.LogInformation("無快取資料，從外部 API 獲取避難所資料");
            return await RefreshCacheAsync();
        }

        /// <summary>
        /// 根據災害類型獲取避難所（使用快取）
        /// </summary>
        public async Task<List<Shelter>> GetSheltersByDisasterTypeAsync(DisasterTypes disasterType)
        {
            // 確保快取存在
            await EnsureCacheExistsAsync();

            // 從記憶體或資料庫快取查詢
            if (_memoryCache.TryGetValue(MEMORY_CACHE_KEY, out List<Shelter>? cachedShelters) && cachedShelters != null)
            {
                return cachedShelters.Where(s => (s.SupportedDisasters & disasterType) != 0).ToList();
            }

            return await _repository.GetSheltersByDisasterTypeAsync(disasterType);
        }

        /// <summary>
        /// 搜尋附近的避難所（使用快取）
        /// </summary>
        public async Task<List<Shelter>> GetNearbySheltersAsync(double latitude, double longitude, double radiusInKm)
        {
            await EnsureCacheExistsAsync();

            if (_memoryCache.TryGetValue(MEMORY_CACHE_KEY, out List<Shelter>? cachedShelters) && cachedShelters != null)
            {
                return cachedShelters
                    .Where(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude) <= radiusInKm)
                    .OrderBy(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude))
                    .ToList();
            }

            return await _repository.GetNearbySheltersAsync(latitude, longitude, radiusInKm);
        }

        /// <summary>
        /// 根據名稱搜尋避難所（使用快取）
        /// </summary>
        public async Task<List<Shelter>> SearchSheltersByNameAsync(string name)
        {
            await EnsureCacheExistsAsync();

            if (_memoryCache.TryGetValue(MEMORY_CACHE_KEY, out List<Shelter>? cachedShelters) && cachedShelters != null)
            {
                return cachedShelters
                    .Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return await _repository.SearchSheltersByNameAsync(name);
        }

        /// <summary>
        /// 獲取有無障礙設施的避難所（使用快取）
        /// </summary>
        public async Task<List<Shelter>> GetAccessibleSheltersAsync()
        {
            await EnsureCacheExistsAsync();

            if (_memoryCache.TryGetValue(MEMORY_CACHE_KEY, out List<Shelter>? cachedShelters) && cachedShelters != null)
            {
                return cachedShelters.Where(s => s.Accesibility).ToList();
            }

            return await _repository.GetAccessibleSheltersAsync();
        }

        /// <summary>
        /// 根據容量篩選避難所（使用快取）
        /// </summary>
        public async Task<List<Shelter>> GetSheltersByMinCapacityAsync(int minCapacity)
        {
            await EnsureCacheExistsAsync();

            if (_memoryCache.TryGetValue(MEMORY_CACHE_KEY, out List<Shelter>? cachedShelters) && cachedShelters != null)
            {
                return cachedShelters
                    .Where(s => s.Capacity >= minCapacity)
                    .OrderByDescending(s => s.Capacity)
                    .ToList();
            }

            return await _repository.GetSheltersByMinCapacityAsync(minCapacity);
        }

        /// <summary>
        /// 獲取統計資訊（使用快取）
        /// </summary>
        public async Task<UnifiedShelterStatistics> GetUnifiedStatisticsAsync()
        {
            var shelters = await GetAllSheltersAsync();

            var naturalDisasterShelters = shelters.Where(s => s.Type != "防空避難所").ToList();
            var airRaidShelters = shelters.Where(s => s.Type == "防空避難所" ||
                                                      s.SupportedDisasters.HasFlag(DisasterTypes.AirRaid)).ToList();

            return new UnifiedShelterStatistics
            {
                NaturalDisasterShelters = new ShelterTypeStatistics
                {
                    TotalCount = naturalDisasterShelters.Count,
                    TotalCapacity = naturalDisasterShelters.Sum(s => s.Capacity),
                    AverageCapacity = naturalDisasterShelters.Any() ? (int)naturalDisasterShelters.Average(s => s.Capacity) : 0,
                    AccessibleCount = naturalDisasterShelters.Count(s => s.Accesibility)
                },
                AirRaidShelters = new ShelterTypeStatistics
                {
                    TotalCount = airRaidShelters.Count,
                    TotalCapacity = airRaidShelters.Sum(s => s.Capacity),
                    AverageCapacity = airRaidShelters.Any() ? (int)airRaidShelters.Average(s => s.Capacity) : 0,
                    AccessibleCount = airRaidShelters.Count(s => s.Accesibility)
                },
                TotalShelters = shelters.Count,
                TotalCapacity = shelters.Sum(s => s.Capacity),
                DisasterSupport = new DisasterSupportStatistics
                {
                    FloodingCount = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Flooding)),
                    EarthquakeCount = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Earthquake)),
                    LandslideCount = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Landslide)),
                    TsunamiCount = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Tsunami)),
                    AirRaidCount = airRaidShelters.Count
                }
            };
        }

        /// <summary>
        /// 強制刷新快取（從外部 API 重新獲取資料）
        /// </summary>
        public async Task<List<Shelter>> RefreshCacheAsync()
        {
            _logger.LogInformation("開始刷新避難所快取...");

            try
            {
                // 從外部 API 獲取最新資料
                var shelters = await _unifiedShelterService.GetAllSheltersAsync();

                // 更新資料庫快取
                await _repository.RefreshAllSheltersAsync(shelters);

                // 更新記憶體快取
                _memoryCache.Set(MEMORY_CACHE_KEY, shelters, _memoryCacheDuration);

                _logger.LogInformation($"快取刷新完成，共 {shelters.Count} 筆資料");

                return shelters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新快取時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 獲取快取資訊
        /// </summary>
        public async Task<CacheInfo> GetCacheInfoAsync()
        {
            var metadata = await _repository.GetCacheMetadataAsync();
            var count = await _repository.GetCachedShelterCountAsync();
            var hasMemoryCache = _memoryCache.TryGetValue(MEMORY_CACHE_KEY, out _);

            return new CacheInfo
            {
                HasDatabaseCache = await _repository.HasCacheAsync(),
                HasMemoryCache = hasMemoryCache,
                CachedRecordCount = count,
                LastUpdated = metadata?.LastUpdated,
                MemoryCacheDurationMinutes = (int)_memoryCacheDuration.TotalMinutes
            };
        }

        /// <summary>
        /// 清除所有快取
        /// </summary>
        public async Task ClearAllCacheAsync()
        {
            _logger.LogInformation("清除所有快取");

            // 清除記憶體快取
            _memoryCache.Remove(MEMORY_CACHE_KEY);

            // 清除資料庫快取
            await _repository.RefreshAllSheltersAsync(new List<Shelter>());

            _logger.LogInformation("所有快取已清除");
        }

        /// <summary>
        /// 確保快取存在，如果不存在則建立
        /// </summary>
        private async Task EnsureCacheExistsAsync()
        {
            if (!await _repository.HasCacheAsync())
            {
                _logger.LogInformation("快取不存在，自動建立快取");
                await RefreshCacheAsync();
            }
        }

        /// <summary>
        /// 計算兩點間的距離（Haversine 公式）
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // 地球半徑（公里）

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }

    /// <summary>
    /// 快取資訊
    /// </summary>
    public class CacheInfo
    {
        public bool HasDatabaseCache { get; set; }
        public bool HasMemoryCache { get; set; }
        public int CachedRecordCount { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int MemoryCacheDurationMinutes { get; set; }
    }
}
