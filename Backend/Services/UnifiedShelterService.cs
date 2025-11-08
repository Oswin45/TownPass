using Backend.Models;

namespace Backend.Services
{
    /// <summary>
    /// 統一避難所服務 - 整合天然災害避難所和防空避難所
    /// Unified Shelter Service - Integrates natural disaster and air raid shelters
    /// </summary>
    public class UnifiedShelterService
    {
        private readonly NaturalDisasterShelterService _naturalDisasterService;
        private readonly AirRaidShelterService _airRaidService;
        private readonly ILogger<UnifiedShelterService> _logger;

        public UnifiedShelterService(
            NaturalDisasterShelterService naturalDisasterService,
            AirRaidShelterService airRaidService,
            ILogger<UnifiedShelterService> logger)
        {
            _naturalDisasterService = naturalDisasterService;
            _airRaidService = airRaidService;
            _logger = logger;
        }

        /// <summary>
        /// 獲取所有避難所（包含天然災害和防空避難所）
        /// Get all shelters (includes natural disaster and air raid shelters)
        /// </summary>
        /// <returns>統一的避難所列表</returns>
        public async Task<List<Shelter>> GetAllSheltersAsync()
        {
            try
            {
                _logger.LogInformation("獲取所有避難所資料...");

                // 並行獲取兩種避難所資料
                var naturalDisasterTask = _naturalDisasterService.FetchAndParseNaturalDisasterSheltersAsync();
                var airRaidTask = _airRaidService.FetchAndParseAirRaidSheltersAsync();

                await Task.WhenAll(naturalDisasterTask, airRaidTask);

                var naturalDisasterShelters = await naturalDisasterTask;
                var airRaidShelters = await airRaidTask;

                // 轉換為統一的 Shelter 模型
                var allShelters = new List<Shelter>();
                allShelters.AddRange(naturalDisasterShelters.Select(s => s.ConvertToShelter()));
                allShelters.AddRange(airRaidShelters.Select(s => s.ConvertToShelter()));

                _logger.LogInformation($"成功獲取 {allShelters.Count} 個避難所 (天然災害: {naturalDisasterShelters.Count}, 防空: {airRaidShelters.Count})");

                return allShelters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取所有避難所時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據災害類型獲取避難所
        /// </summary>
        /// <param name="disasterType">災害類型</param>
        /// <returns>支援該災害類型的避難所列表</returns>
        public async Task<List<Shelter>> GetSheltersByDisasterTypeAsync(DisasterTypes disasterType)
        {
            try
            {
                _logger.LogInformation($"根據災害類型獲取避難所: {disasterType}");

                var shelters = new List<Shelter>();

                // 如果是防空避難所
                if (disasterType.HasFlag(DisasterTypes.AirRaid))
                {
                    var airRaidShelters = await _airRaidService.FetchAndParseAirRaidSheltersAsync();
                    shelters.AddRange(airRaidShelters.Select(s => s.ConvertToShelter()));
                }

                // 如果是天然災害避難所
                if (disasterType.HasFlag(DisasterTypes.Flooding) ||
                    disasterType.HasFlag(DisasterTypes.Earthquake) ||
                    disasterType.HasFlag(DisasterTypes.Landslide) ||
                    disasterType.HasFlag(DisasterTypes.Tsunami))
                {
                    var naturalDisasterShelters = await _naturalDisasterService.GetSheltersByDisasterTypeAsync(disasterType);
                    shelters.AddRange(naturalDisasterShelters.Select(s => s.ConvertToShelter()));
                }

                return shelters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"根據災害類型 {disasterType} 獲取避難所時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據地理位置搜尋附近的所有類型避難所
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <param name="longitude">經度</param>
        /// <param name="radiusInKm">搜尋半徑（公里）</param>
        /// <returns>範圍內的所有避難所列表（依距離排序）</returns>
        public async Task<List<Shelter>> GetNearbySheltersAsync(double latitude, double longitude, double radiusInKm)
        {
            try
            {
                _logger.LogInformation($"搜尋座標 ({latitude}, {longitude}) 附近 {radiusInKm} 公里內的避難所");

                // 獲取防空避難所（已有座標資訊）
                var airRaidShelters = await _airRaidService.GetNearbySheltersAsync(latitude, longitude, radiusInKm);
                var nearbyShelters = airRaidShelters.Select(s => s.ConvertToShelter()).ToList();

                // TODO: 天然災害避難所需要地理編碼才能進行距離計算
                // 目前僅包含防空避難所

                _logger.LogInformation($"找到 {nearbyShelters.Count} 個附近的避難所");

                return nearbyShelters
                    .OrderBy(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋附近避難所時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據名稱搜尋避難所（包含所有類型）
        /// </summary>
        /// <param name="name">搜尋關鍵字</param>
        /// <returns>包含關鍵字的避難所列表</returns>
        public async Task<List<Shelter>> SearchSheltersByNameAsync(string name)
        {
            try
            {
                _logger.LogInformation($"搜尋名稱包含 '{name}' 的避難所");

                var naturalDisasterTask = _naturalDisasterService.SearchSheltersByNameAsync(name);
                var airRaidTask = Task.FromResult(
                    _airRaidService.FetchAndParseAirRaidSheltersAsync().Result
                        .Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                        .ToList()
                );

                await Task.WhenAll(naturalDisasterTask);

                var shelters = new List<Shelter>();
                shelters.AddRange((await naturalDisasterTask).Select(s => s.ConvertToShelter()));
                shelters.AddRange((await airRaidTask).Select(s => s.ConvertToShelter()));

                return shelters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜尋名稱 '{name}' 時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 獲取統一的避難所統計資訊
        /// </summary>
        /// <returns>包含所有類型避難所的統計資訊</returns>
        public async Task<UnifiedShelterStatistics> GetUnifiedStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("獲取統一避難所統計資訊");

                var naturalStatsTask = _naturalDisasterService.GetShelterStatisticsAsync();
                var airRaidTask = _airRaidService.FetchAndParseAirRaidSheltersAsync();

                await Task.WhenAll(naturalStatsTask, airRaidTask);

                var naturalStats = await naturalStatsTask;
                var airRaidShelters = await airRaidTask;

                return new UnifiedShelterStatistics
                {
                    NaturalDisasterShelters = new ShelterTypeStatistics
                    {
                        TotalCount = naturalStats.TotalShelters,
                        TotalCapacity = naturalStats.TotalCapacity,
                        AverageCapacity = naturalStats.AverageCapacity,
                        AccessibleCount = naturalStats.AccessibleCount
                    },
                    AirRaidShelters = new ShelterTypeStatistics
                    {
                        TotalCount = airRaidShelters.Count,
                        TotalCapacity = airRaidShelters.Sum(s => s.Capacity),
                        AverageCapacity = airRaidShelters.Any() ? (int)airRaidShelters.Average(s => s.Capacity) : 0,
                        AccessibleCount = 0 // 防空避難所資料中無此資訊
                    },
                    TotalShelters = naturalStats.TotalShelters + airRaidShelters.Count,
                    TotalCapacity = naturalStats.TotalCapacity + airRaidShelters.Sum(s => s.Capacity),
                    DisasterSupport = new DisasterSupportStatistics
                    {
                        FloodingCount = naturalStats.FloodingSupportCount,
                        EarthquakeCount = naturalStats.EarthquakeSupportCount,
                        LandslideCount = naturalStats.LandslideSupportCount,
                        TsunamiCount = naturalStats.TsunamiSupportCount,
                        AirRaidCount = airRaidShelters.Count
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取統一統計資訊時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 計算兩點間的距離（使用 Haversine 公式）
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
    /// 統一避難所統計資訊
    /// </summary>
    public class UnifiedShelterStatistics
    {
        public ShelterTypeStatistics NaturalDisasterShelters { get; set; } = new();
        public ShelterTypeStatistics AirRaidShelters { get; set; } = new();
        public int TotalShelters { get; set; }
        public int TotalCapacity { get; set; }
        public DisasterSupportStatistics DisasterSupport { get; set; } = new();
    }

    /// <summary>
    /// 單一類型避難所統計
    /// </summary>
    public class ShelterTypeStatistics
    {
        public int TotalCount { get; set; }
        public int TotalCapacity { get; set; }
        public int AverageCapacity { get; set; }
        public int AccessibleCount { get; set; }
    }

    /// <summary>
    /// 災害支援統計
    /// </summary>
    public class DisasterSupportStatistics
    {
        public int FloodingCount { get; set; }
        public int EarthquakeCount { get; set; }
        public int LandslideCount { get; set; }
        public int TsunamiCount { get; set; }
        public int AirRaidCount { get; set; }
    }
}
