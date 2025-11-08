using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// 管理員控制器 - 用於管理避難所快取和系統維護
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly CachedUnifiedShelterService _cachedShelterService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            CachedUnifiedShelterService cachedShelterService,
            ILogger<AdminController> logger)
        {
            _cachedShelterService = cachedShelterService;
            _logger = logger;
        }

        /// <summary>
        /// 獲取快取狀態資訊
        /// GET /api/Admin/cache-status
        /// </summary>
        [HttpGet("cache-status")]
        public async Task<IActionResult> GetCacheStatus()
        {
            try
            {
                _logger.LogInformation("查詢快取狀態");
                var cacheInfo = await _cachedShelterService.GetCacheInfoAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        databaseCache = new
                        {
                            enabled = cacheInfo.HasDatabaseCache,
                            recordCount = cacheInfo.CachedRecordCount,
                            lastUpdated = cacheInfo.LastUpdated,
                            ageInHours = cacheInfo.LastUpdated.HasValue 
                                ? (DateTime.UtcNow - cacheInfo.LastUpdated.Value).TotalHours 
                                : (double?)null
                        },
                        memoryCache = new
                        {
                            enabled = cacheInfo.HasMemoryCache,
                            durationMinutes = cacheInfo.MemoryCacheDurationMinutes
                        },
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取快取狀態時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 強制刷新避難所資料快取
        /// POST /api/Admin/refresh-cache
        /// </summary>
        [HttpPost("refresh-cache")]
        public async Task<IActionResult> RefreshCache()
        {
            try
            {
                _logger.LogInformation("管理員觸發快取刷新");
                
                var startTime = DateTime.UtcNow;
                var shelters = await _cachedShelterService.RefreshCacheAsync();
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;

                return Ok(new
                {
                    success = true,
                    message = "快取刷新成功",
                    data = new
                    {
                        totalShelters = shelters.Count,
                        refreshDurationSeconds = Math.Round(duration, 2),
                        timestamp = DateTime.UtcNow,
                        sheltersByType = new
                        {
                            naturalDisaster = shelters.Count(s => s.Type != "防空避難所"),
                            airRaid = shelters.Count(s => s.Type == "防空避難所" || 
                                                         s.SupportedDisasters.HasFlag(DisasterTypes.AirRaid))
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新快取時發生錯誤");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "刷新快取失敗", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// 清除所有快取
        /// DELETE /api/Admin/clear-cache
        /// </summary>
        [HttpDelete("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                _logger.LogWarning("管理員觸發清除所有快取");
                
                await _cachedShelterService.ClearAllCacheAsync();

                return Ok(new
                {
                    success = true,
                    message = "所有快取已清除",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除快取時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 執行完整的資料更新流程（清除舊快取 + 重新抓取資料）
        /// POST /api/Admin/full-update
        /// </summary>
        [HttpPost("full-update")]
        public async Task<IActionResult> FullUpdate()
        {
            try
            {
                _logger.LogInformation("管理員觸發完整資料更新");
                
                var startTime = DateTime.UtcNow;

                // 先清除快取
                await _cachedShelterService.ClearAllCacheAsync();
                
                // 重新抓取資料
                var shelters = await _cachedShelterService.RefreshCacheAsync();
                
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;

                return Ok(new
                {
                    success = true,
                    message = "完整資料更新成功",
                    data = new
                    {
                        totalShelters = shelters.Count,
                        updateDurationSeconds = Math.Round(duration, 2),
                        timestamp = DateTime.UtcNow,
                        details = new
                        {
                            naturalDisaster = shelters.Count(s => s.Type != "防空避難所"),
                            airRaid = shelters.Count(s => s.Type == "防空避難所" || 
                                                         s.SupportedDisasters.HasFlag(DisasterTypes.AirRaid)),
                            withAccessibility = shelters.Count(s => s.Accesibility),
                            totalCapacity = shelters.Sum(s => s.Capacity),
                            averageCapacity = shelters.Any() ? (int)shelters.Average(s => s.Capacity) : 0
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完整資料更新時發生錯誤");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "完整資料更新失敗", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// 獲取系統健康狀態
        /// GET /api/Admin/health
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                var cacheInfo = await _cachedShelterService.GetCacheInfoAsync();
                
                var isHealthy = cacheInfo.HasDatabaseCache && cacheInfo.CachedRecordCount > 0;
                var cacheAge = cacheInfo.LastUpdated.HasValue 
                    ? (DateTime.UtcNow - cacheInfo.LastUpdated.Value).TotalHours 
                    : (double?)null;

                // 如果快取超過 24 小時，發出警告
                var needsRefresh = cacheAge.HasValue && cacheAge.Value > 24;

                return Ok(new
                {
                    success = true,
                    healthy = isHealthy,
                    data = new
                    {
                        status = isHealthy ? "healthy" : "unhealthy",
                        warnings = needsRefresh ? new[] { "快取資料超過 24 小時，建議刷新" } : Array.Empty<string>(),
                        cache = new
                        {
                            recordCount = cacheInfo.CachedRecordCount,
                            ageInHours = cacheAge.HasValue ? Math.Round(cacheAge.Value, 2) : (double?)null,
                            lastUpdated = cacheInfo.LastUpdated
                        },
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取健康狀態時發生錯誤");
                return StatusCode(500, new 
                { 
                    success = false, 
                    healthy = false,
                    message = ex.Message 
                });
            }
        }

        /// <summary>
        /// 獲取快取統計資訊
        /// GET /api/Admin/cache-statistics
        /// </summary>
        [HttpGet("cache-statistics")]
        public async Task<IActionResult> GetCacheStatistics()
        {
            try
            {
                _logger.LogInformation("獲取快取統計資訊");
                
                var statistics = await _cachedShelterService.GetUnifiedStatisticsAsync();
                var cacheInfo = await _cachedShelterService.GetCacheInfoAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        cache = new
                        {
                            totalRecords = cacheInfo.CachedRecordCount,
                            lastUpdated = cacheInfo.LastUpdated,
                            cacheAge = cacheInfo.LastUpdated.HasValue 
                                ? $"{Math.Round((DateTime.UtcNow - cacheInfo.LastUpdated.Value).TotalHours, 1)} 小時"
                                : "未知"
                        },
                        shelters = new
                        {
                            total = statistics.TotalShelters,
                            totalCapacity = statistics.TotalCapacity,
                            byType = new
                            {
                                naturalDisaster = statistics.NaturalDisasterShelters,
                                airRaid = statistics.AirRaidShelters
                            },
                            byDisaster = statistics.DisasterSupport
                        },
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取快取統計資訊時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
