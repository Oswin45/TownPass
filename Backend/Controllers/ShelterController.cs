using System.Text.Json;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShelterController : ControllerBase
    {
        private readonly ILogger<ShelterController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ShelterController(ILogger<ShelterController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 從 API 獲取並轉換收容所資料
        /// </summary>
        private async Task<List<Shelter>> FetchAndConvertShelters()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = "https://data.taipei/api/v1/dataset/4c92dbd4-d259-495a-8390-52628119a4dd?scope=resourceAquire&limit=1000";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var shelterResponse = JsonSerializer.Deserialize<NaturalDisasterResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return shelterResponse?.Result?.Results?.Select(x => x.ConvertToShelter()).ToList() ?? new List<Shelter>();
        }

        /// <summary>
        /// 獲取所有收容所
        /// GET /api/Shelter/all
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllShelters()
        {
            try
            {
                _logger.LogInformation("獲取所有避難收容所資料");
                var shelters = await FetchAndConvertShelters();
                
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
        /// 根據災害類型篩選收容所
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
                var shelters = await FetchAndConvertShelters();
                var filtered = shelters.Where(s => s.SupportedDisasters.HasFlag(disasterType)).ToList();

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
        /// 根據區域篩選收容所
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
                
                // 需要從原始資料獲取區域資訊
                var httpClient = _httpClientFactory.CreateClient();
                var url = "https://data.taipei/api/v1/dataset/4c92dbd4-d259-495a-8390-52628119a4dd?scope=resourceAquire&limit=1000";
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var shelterResponse = JsonSerializer.Deserialize<NaturalDisasterResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var filteredOriginal = shelterResponse?.Result?.Results?
                    .Where(s => s.District?.Contains(district, StringComparison.OrdinalIgnoreCase) == true)
                    .Select(s => s.ConvertToShelter())
                    .ToList() ?? new List<Shelter>();

                return Ok(new
                {
                    success = true,
                    district = district,
                    count = filteredOriginal.Count,
                    data = filteredOriginal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據區域篩選收容所時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 根據最小容量篩選收容所
        /// GET /api/Shelter/by-capacity?minCapacity=100
        /// </summary>
        [HttpGet("by-capacity")]
        public async Task<IActionResult> GetSheltersByCapacity([FromQuery] int minCapacity = 0)
        {
            try
            {
                _logger.LogInformation($"根據最小容量篩選收容所: {minCapacity}");
                var shelters = await FetchAndConvertShelters();
                var filtered = shelters.Where(s => s.Capacity >= minCapacity).OrderByDescending(s => s.Capacity).ToList();

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
        /// 根據名稱搜尋收容所
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
                var shelters = await FetchAndConvertShelters();
                var filtered = shelters.Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

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
        /// 獲取有無障礙設施的收容所
        /// GET /api/Shelter/accessible
        /// </summary>
        [HttpGet("accessible")]
        public async Task<IActionResult> GetAccessibleShelters()
        {
            try
            {
                _logger.LogInformation("獲取有無障礙設施的收容所");
                var shelters = await FetchAndConvertShelters();
                var filtered = shelters.Where(s => s.Accesibility).ToList();

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
        /// 獲取收容所統計資訊
        /// GET /api/Shelter/statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetShelterStatistics()
        {
            try
            {
                _logger.LogInformation("獲取收容所統計資訊");
                var shelters = await FetchAndConvertShelters();

                var stats = new
                {
                    success = true,
                    totalShelters = shelters.Count,
                    totalCapacity = shelters.Sum(s => s.Capacity),
                    averageCapacity = shelters.Any() ? (int)shelters.Average(s => s.Capacity) : 0,
                    accessibleCount = shelters.Count(s => s.Accesibility),
                    disasterSupport = new
                    {
                        flooding = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Flooding)),
                        earthquake = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Earthquake)),
                        landslide = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Landslide)),
                        tsunami = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.Tsunami)),
                        airRaid = shelters.Count(s => s.SupportedDisasters.HasFlag(DisasterTypes.AirRaid))
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
    }
}
