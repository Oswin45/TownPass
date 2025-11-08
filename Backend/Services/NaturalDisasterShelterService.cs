using System.Text.Json;
using Backend.Models;

namespace Backend.Services
{
    /// <summary>
    /// 天然災害避難所資料服務
    /// Natural Disaster Shelter Data Service
    /// </summary>
    public class NaturalDisasterShelterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NaturalDisasterShelterService> _logger;
        private const string API_ENDPOINT = "https://data.taipei/api/v1/dataset/4c92dbd4-d259-495a-8390-52628119a4dd?scope=resourceAquire&limit=1000";

        public NaturalDisasterShelterService(HttpClient httpClient, ILogger<NaturalDisasterShelterService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// 從台北市開放資料平台獲取並解析天然災害避難所資料
        /// Fetch and parse natural disaster shelter data from Taipei Open Data Platform
        /// </summary>
        /// <returns>天然災害避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> FetchAndParseNaturalDisasterSheltersAsync()
        {
            try
            {
                _logger.LogInformation("開始從 API 端點獲取天然災害避難所資料...");
                
                var response = await _httpClient.GetAsync(API_ENDPOINT);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    _logger.LogWarning("API 回應內容為空");
                    return new List<NaturalDisasterShelter>();
                }

                _logger.LogInformation($"成功下載資料，大小: {jsonString.Length} 字元");

                var shelterResponse = JsonSerializer.Deserialize<NaturalDisasterResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var shelters = shelterResponse?.Result?.Results ?? new List<NaturalDisasterShelter>();
                
                _logger.LogInformation($"成功解析 {shelters.Count} 個天然災害避難所");
                
                return shelters;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP 請求失敗");
                throw new Exception("無法從遠端伺服器獲取資料", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON 解析失敗");
                throw new Exception("回應資料格式不正確或無法解析", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取或解析天然災害避難所資料時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 從自訂 URL 獲取並解析天然災害避難所資料
        /// </summary>
        /// <param name="url">資料來源 URL</param>
        /// <returns>天然災害避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> FetchAndParseNaturalDisasterSheltersAsync(string url)
        {
            try
            {
                _logger.LogInformation($"從自訂 URL 獲取資料: {url}");
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    _logger.LogWarning("API 回應內容為空");
                    return new List<NaturalDisasterShelter>();
                }

                var shelterResponse = JsonSerializer.Deserialize<NaturalDisasterResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return shelterResponse?.Result?.Results ?? new List<NaturalDisasterShelter>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"從 URL {url} 獲取或解析資料時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 獲取特定災害類型的避難所
        /// </summary>
        /// <param name="disasterType">災害類型</param>
        /// <returns>支援該災害類型的避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> GetSheltersByDisasterTypeAsync(DisasterTypes disasterType)
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            
            return allShelters.Where(shelter =>
            {
                return disasterType switch
                {
                    DisasterTypes.Flooding => IsSupported(shelter.FloodDisaster),
                    DisasterTypes.Earthquake => IsSupported(shelter.EarthquakeDisaster),
                    DisasterTypes.Landslide => IsSupported(shelter.Landslide),
                    DisasterTypes.Tsunami => IsSupported(shelter.Tsunami),
                    _ => false
                };
            }).ToList();
        }

        /// <summary>
        /// 獲取特定區域的避難所
        /// </summary>
        /// <param name="district">區域名稱</param>
        /// <returns>該區域的避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> GetSheltersByDistrictAsync(string district)
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            return allShelters
                .Where(s => s.District != null && s.District.Contains(district, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 獲取特定村里的避難所
        /// </summary>
        /// <param name="village">村里名稱</param>
        /// <returns>該村里的避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> GetSheltersByVillageAsync(string village)
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            return allShelters
                .Where(s => s.Village != null && s.Village.Contains(village, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 根據最小容量篩選避難所
        /// </summary>
        /// <param name="minCapacity">最小容納人數</param>
        /// <returns>符合容量要求的避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> GetSheltersByMinCapacityAsync(int minCapacity)
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            return allShelters
                .Where(s => int.TryParse(s.Capacity?.Trim(), out int capacity) && capacity >= minCapacity)
                .OrderByDescending(s => int.TryParse(s.Capacity?.Trim(), out int capacity) ? capacity : 0)
                .ToList();
        }

        /// <summary>
        /// 根據名稱搜尋避難所
        /// </summary>
        /// <param name="name">搜尋關鍵字</param>
        /// <returns>包含關鍵字的避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> SearchSheltersByNameAsync(string name)
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            return allShelters
                .Where(s => s.Name != null && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 獲取有無障礙設施的避難所
        /// </summary>
        /// <returns>有無障礙設施的避難所列表</returns>
        public async Task<List<NaturalDisasterShelter>> GetAccessibleSheltersAsync()
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            return allShelters
                .Where(s => !string.IsNullOrEmpty(s.AccessibleFacilities) && s.AccessibleFacilities == "是")
                .ToList();
        }

        /// <summary>
        /// 獲取避難所統計資訊
        /// </summary>
        /// <returns>統計資訊物件</returns>
        public async Task<ShelterStatistics> GetShelterStatisticsAsync()
        {
            var allShelters = await FetchAndParseNaturalDisasterSheltersAsync();
            
            var capacities = allShelters
                .Where(s => int.TryParse(s.Capacity?.Trim(), out _))
                .Select(s => int.Parse(s.Capacity!.Trim()))
                .ToList();

            return new ShelterStatistics
            {
                TotalShelters = allShelters.Count,
                TotalCapacity = capacities.Sum(),
                AverageCapacity = capacities.Any() ? (int)capacities.Average() : 0,
                AccessibleCount = allShelters.Count(s => !string.IsNullOrEmpty(s.AccessibleFacilities) && s.AccessibleFacilities == "是"),
                FloodingSupportCount = allShelters.Count(s => IsSupported(s.FloodDisaster)),
                EarthquakeSupportCount = allShelters.Count(s => IsSupported(s.EarthquakeDisaster)),
                LandslideSupportCount = allShelters.Count(s => IsSupported(s.Landslide)),
                TsunamiSupportCount = allShelters.Count(s => IsSupported(s.Tsunami))
            };
        }

        /// <summary>
        /// 檢查災害支援狀態
        /// </summary>
        private bool IsSupported(string? value)
        {
            return !string.IsNullOrEmpty(value) && (value == "Y" || value == "是" || value == "備用");
        }
    }

    /// <summary>
    /// 避難所統計資訊
    /// </summary>
    public class ShelterStatistics
    {
        public int TotalShelters { get; set; }
        public int TotalCapacity { get; set; }
        public int AverageCapacity { get; set; }
        public int AccessibleCount { get; set; }
        public int FloodingSupportCount { get; set; }
        public int EarthquakeSupportCount { get; set; }
        public int LandslideSupportCount { get; set; }
        public int TsunamiSupportCount { get; set; }
    }
}
