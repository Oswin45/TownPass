using System.Xml.Serialization;
using Backend.Models;

namespace Backend.Services
{
    /// <summary>
    /// 防空避難所資料服務
    /// Air Raid Shelter Data Service
    /// </summary>
    public class AirRaidShelterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AirRaidShelterService> _logger;
        private const string KML_ENDPOINT = "https://www.google.com/maps/d/u/0/kml?mid=1kXkkEoggmJqoYFk-jdxinZtOzne4CrNK&resourcekey&forcekml=1";

        public AirRaidShelterService(HttpClient httpClient, ILogger<AirRaidShelterService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// 從 Google Maps KML 端點獲取並解析防空避難所資料
        /// Fetch and parse air raid shelter data from Google Maps KML endpoint
        /// </summary>
        /// <returns>防空避難所列表</returns>
        public async Task<List<AirRaidShelter>> FetchAndParseAirRaidSheltersAsync()
        {
            try
            {
                _logger.LogInformation("開始從 KML 端點獲取防空避難所資料...");
                
                // 下載 KML 資料
                var kmlContent = await _httpClient.GetStringAsync(KML_ENDPOINT);
                
                if (string.IsNullOrWhiteSpace(kmlContent))
                {
                    _logger.LogWarning("KML 內容為空");
                    return new List<AirRaidShelter>();
                }

                _logger.LogInformation($"成功下載 KML 資料，大小: {kmlContent.Length} 字元");

                // 解析 KML
                var shelters = ParseKmlToShelters(kmlContent);
                
                _logger.LogInformation($"成功解析 {shelters.Count} 個防空避難所");
                
                return shelters;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP 請求失敗");
                throw new Exception("無法從遠端伺服器獲取 KML 資料", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取或解析 KML 資料時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 從指定 URL 獲取並解析防空避難所資料
        /// </summary>
        /// <param name="url">KML 資料來源 URL</param>
        /// <returns>防空避難所列表</returns>
        public async Task<List<AirRaidShelter>> FetchAndParseAirRaidSheltersAsync(string url)
        {
            try
            {
                _logger.LogInformation($"從自訂 URL 獲取 KML 資料: {url}");
                
                var kmlContent = await _httpClient.GetStringAsync(url);
                
                if (string.IsNullOrWhiteSpace(kmlContent))
                {
                    _logger.LogWarning("KML 內容為空");
                    return new List<AirRaidShelter>();
                }

                return ParseKmlToShelters(kmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"從 URL {url} 獲取或解析 KML 資料時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 解析 KML 字串為防空避難所列表
        /// Parse KML string to list of air raid shelters
        /// </summary>
        /// <param name="kmlContent">KML 內容字串</param>
        /// <returns>防空避難所列表</returns>
        public List<AirRaidShelter> ParseKmlToShelters(string kmlContent)
        {
            var shelters = new List<AirRaidShelter>();

            try
            {
                // 建立 XML 序列化器
                var serializer = new XmlSerializer(typeof(KmlRoot));
                
                using var reader = new StringReader(kmlContent);
                var kmlRoot = (KmlRoot?)serializer.Deserialize(reader);

                if (kmlRoot?.Document == null)
                {
                    _logger.LogWarning("KML Document 為空");
                    return shelters;
                }

                // 遍歷所有 Folder 和 Placemark
                if (kmlRoot.Document.Folders != null)
                {
                    foreach (var folder in kmlRoot.Document.Folders)
                    {
                        if (folder.Placemarks == null) continue;

                        foreach (var placemark in folder.Placemarks)
                        {
                            try
                            {
                                var shelter = ConvertPlacemarkToShelter(placemark);
                                if (shelter != null)
                                {
                                    shelters.Add(shelter);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"解析 Placemark '{placemark.Name}' 時發生錯誤，跳過此項目");
                            }
                        }
                    }
                }

                // 處理沒有 Folder 的情況 (直接在 Document 下的 Placemarks)
                // Note: 根據提供的 KML 結構，Placemarks 在 Folder 中，但這裡保留彈性
                
                return shelters;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "XML 反序列化失敗");
                throw new Exception("KML 格式不正確或無法解析", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 KML 時發生未預期的錯誤");
                throw;
            }
        }

        /// <summary>
        /// 將 KML Placemark 轉換為 AirRaidShelter
        /// </summary>
        private AirRaidShelter? ConvertPlacemarkToShelter(KmlPlacemark placemark)
        {
            if (placemark.ExtendedData == null)
            {
                _logger.LogWarning($"Placemark '{placemark.Name}' 沒有 ExtendedData");
                return null;
            }

            // 使用 ExtendedData 的轉換方法
            var shelter = placemark.ExtendedData.ToAirRaidShelter(
                placemark.Name,
                placemark.Point
            );

            return shelter;
        }

        /// <summary>
        /// 獲取特定轄區的防空避難所
        /// </summary>
        /// <param name="precinct">轄區名稱</param>
        /// <returns>該轄區的防空避難所列表</returns>
        public async Task<List<AirRaidShelter>> GetSheltersByPrecinctAsync(string precinct)
        {
            var allShelters = await FetchAndParseAirRaidSheltersAsync();
            return allShelters
                .Where(s => s.Precinct != null && s.Precinct.Contains(precinct, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 獲取特定村里的防空避難所
        /// </summary>
        /// <param name="village">村里名稱</param>
        /// <returns>該村里的防空避難所列表</returns>
        public async Task<List<AirRaidShelter>> GetSheltersByVillageAsync(string village)
        {
            var allShelters = await FetchAndParseAirRaidSheltersAsync();
            return allShelters
                .Where(s => s.Village != null && s.Village.Contains(village, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 依據座標和半徑搜尋附近的防空避難所
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <param name="longitude">經度</param>
        /// <param name="radiusInKm">搜尋半徑（公里）</param>
        /// <returns>範圍內的防空避難所列表</returns>
        public async Task<List<AirRaidShelter>> GetNearbySheltersAsync(double latitude, double longitude, double radiusInKm)
        {
            var allShelters = await FetchAndParseAirRaidSheltersAsync();
            
            return allShelters
                .Where(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude) <= radiusInKm)
                .OrderBy(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude))
                .ToList();
        }

        /// <summary>
        /// 計算兩點間的距離（使用 Haversine 公式）
        /// Calculate distance between two points using Haversine formula
        /// </summary>
        /// <returns>距離（公里）</returns>
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
}
