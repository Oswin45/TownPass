using System.Text.Json;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShelterController : ControllerBase
    {
        private readonly ILogger<ShelterController> _logger;
        private readonly CachedUnifiedShelterService _cachedShelterService;

        public ShelterController(
            ILogger<ShelterController> logger,
            CachedUnifiedShelterService cachedShelterService)
        {
            _logger = logger;
            _cachedShelterService = cachedShelterService;
        }

        /// <summary>
        /// 從快取獲取所有類型的避難所資料
        /// </summary>
        private async Task<List<Shelter>> FetchAllShelters()
        {
            return await _cachedShelterService.GetAllSheltersAsync();
        }

        /// <summary>
        /// 獲取所有收容所（包含天然災害和防空避難所）
        /// GET /api/Shelter/all
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllShelters()
        {
            try
            {
                _logger.LogInformation("獲取所有避難收容所資料（包含天然災害和防空避難所）");
                var shelters = await _cachedShelterService.GetAllSheltersAsync();
                
                return Ok(new
                {
                    success = true,
                    count = shelters.Count,
                    data = shelters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取避難收容所資料時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 根據災害類型篩選收容所（包含所有類型避難所）
        /// GET /api/Shelter/by-disaster?type=Flooding
        /// 可用類型: None, Flooding, Earthquake, Landslide, Tsunami, AirRaid
        /// </summary>
        [HttpGet("by-disaster")]
        public async Task<IActionResult> GetSheltersByDisasterType([FromQuery] string type)
        {
            try
            {
                if (!Enum.TryParse<DisasterTypes>(type, true, out var disasterType))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "無效的災害類型。可用類型: None, Flooding, Earthquake, Landslide, Tsunami, AirRaid"
                    });
                }

                _logger.LogInformation($"根據災害類型篩選收容所: {disasterType}");
                
                // 使用統一服務獲取特定災害類型的避難所
                var filtered = await _cachedShelterService.GetSheltersByDisasterTypeAsync(disasterType);

                return Ok(new
                {
                    success = true,
                    disasterType = disasterType.ToString(),
                    count = filtered.Count,
                    data = filtered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據災害類型篩選收容所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 根據區域篩選收容所（僅支援天然災害避難所）
        /// GET /api/Shelter/by-district?district=中正區
        /// </summary>
        [HttpGet("by-district")]
        public async Task<IActionResult> GetSheltersByDistrict([FromQuery] string district)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(district))
                {
                    return BadRequest(new { success = false, message = "請提供區域名稱" });
                }

                _logger.LogInformation($"根據區域篩選收容所: {district}");
                
                // 區域篩選僅適用於天然災害避難所
                var allShelters = await _cachedShelterService.GetAllSheltersAsync();
                var filtered = allShelters
                    .Where(s => s.Address != null && s.Address.Contains(district, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Ok(new
                {
                    success = true,
                    district = district,
                    count = filtered.Count,
                    data = filtered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據區域篩選收容所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 根據最小容量篩選收容所（包含所有類型避難所）
        /// GET /api/Shelter/by-capacity?minCapacity=100
        /// </summary>
        [HttpGet("by-capacity")]
        public async Task<IActionResult> GetSheltersByCapacity([FromQuery] int minCapacity = 0)
        {
            try
            {
                _logger.LogInformation($"根據最小容量篩選收容所: {minCapacity}");
                var allShelters = await _cachedShelterService.GetAllSheltersAsync();
                var filtered = allShelters
                    .Where(s => s.Capacity >= minCapacity)
                    .OrderByDescending(s => s.Capacity)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    minCapacity = minCapacity,
                    count = filtered.Count,
                    data = filtered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據容量篩選收容所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 根據名稱搜尋收容所（包含所有類型避難所）
        /// GET /api/Shelter/search?name=學校
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchSheltersByName([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { success = false, message = "請提供搜尋關鍵字" });
                }

                _logger.LogInformation($"搜尋收容所: {name}");
                var filtered = await _cachedShelterService.SearchSheltersByNameAsync(name);

                return Ok(new
                {
                    success = true,
                    keyword = name,
                    count = filtered.Count,
                    data = filtered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋收容所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 獲取有無障礙設施的收容所（包含所有類型避難所）
        /// GET /api/Shelter/accessible
        /// </summary>
        [HttpGet("accessible")]
        public async Task<IActionResult> GetAccessibleShelters()
        {
            try
            {
                _logger.LogInformation("獲取有無障礙設施的收容所");
                var allShelters = await _cachedShelterService.GetAllSheltersAsync();
                var filtered = allShelters.Where(s => s.Accesibility).ToList();

                return Ok(new
                {
                    success = true,
                    count = filtered.Count,
                    data = filtered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取無障礙收容所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 獲取收容所統計資訊（包含所有類型避難所）
        /// GET /api/Shelter/statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetShelterStatistics()
        {
            try
            {
                _logger.LogInformation("獲取收容所統計資訊");
                var statistics = await _cachedShelterService.GetUnifiedStatisticsAsync();
                var shelters = await _cachedShelterService.GetAllSheltersAsync();

                var stats = new
                {
                    success = true,
                    totalShelters = statistics.TotalShelters,
                    totalCapacity = statistics.TotalCapacity,
                    shelterTypes = new
                    {
                        naturalDisaster = statistics.NaturalDisasterShelters,
                        airRaid = statistics.AirRaidShelters
                    },
                    disasterSupport = new
                    {
                        flooding = statistics.DisasterSupport.FloodingCount,
                        earthquake = statistics.DisasterSupport.EarthquakeCount,
                        landslide = statistics.DisasterSupport.LandslideCount,
                        tsunami = statistics.DisasterSupport.TsunamiCount,
                        airRaid = statistics.DisasterSupport.AirRaidCount
                    },
                    largestShelter = shelters.OrderByDescending(s => s.Capacity).FirstOrDefault(),
                    smallestShelter = shelters.Where(s => s.Capacity > 0).OrderBy(s => s.Capacity).FirstOrDefault()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取統計資訊時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 根據地理位置搜尋附近的避難所（包含所有類型）
        /// GET /api/Shelter/nearby?latitude=25.060459&longitude=121.509074&radius=2
        /// </summary>
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyShelters(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius = 5.0)
        {
            try
            {
                // 驗證輸入
                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { success = false, message = "緯度必須在 -90 到 90 之間" });
                }

                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { success = false, message = "經度必須在 -180 到 180 之間" });
                }

                if (radius <= 0 || radius > 100)
                {
                    return BadRequest(new { success = false, message = "半徑必須在 0 到 100 公里之間" });
                }

                _logger.LogInformation($"搜尋座標 ({latitude}, {longitude}) 附近 {radius} 公里內的避難所");
                var shelters = await _cachedShelterService.GetNearbySheltersAsync(latitude, longitude, radius);

                return Ok(new
                {
                    success = true,
                    searchLocation = new { latitude, longitude },
                    radiusInKm = radius,
                    count = shelters.Count,
                    data = shelters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋附近避難所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
