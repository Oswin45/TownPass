using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.Models;

namespace Backend.Controllers
{
    /// <summary>
    /// 防空避難所 API 控制器
    /// Air Raid Shelter API Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AirRaidShelterController : ControllerBase
    {
        private readonly AirRaidShelterService _shelterService;
        private readonly ILogger<AirRaidShelterController> _logger;

        public AirRaidShelterController(
            AirRaidShelterService shelterService,
            ILogger<AirRaidShelterController> logger)
        {
            _shelterService = shelterService;
            _logger = logger;
        }

        /// <summary>
        /// 獲取所有防空避難所資料
        /// GET: api/airraidshelter
        /// </summary>
        /// <returns>所有防空避難所列表</returns>
        [HttpGet]
        public async Task<ActionResult<List<AirRaidShelter>>> GetAllShelters()
        {
            try
            {
                var shelters = await _shelterService.FetchAndParseAirRaidSheltersAsync();
                return Ok(shelters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取防空避難所資料失敗");
                return StatusCode(500, new { error = "獲取資料時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 依據轄區搜尋防空避難所
        /// GET: api/airraidshelter/precinct/{precinct}
        /// </summary>
        /// <param name="precinct">轄區名稱（例如：大同分局）</param>
        /// <returns>該轄區的防空避難所列表</returns>
        [HttpGet("precinct/{precinct}")]
        public async Task<ActionResult<List<AirRaidShelter>>> GetSheltersByPrecinct(string precinct)
        {
            try
            {
                var shelters = await _shelterService.GetSheltersByPrecinctAsync(precinct);
                
                if (shelters.Count == 0)
                {
                    return NotFound(new { message = $"找不到轄區 '{precinct}' 的防空避難所" });
                }

                return Ok(shelters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜尋轄區 '{precinct}' 的防空避難所失敗");
                return StatusCode(500, new { error = "搜尋資料時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 依據村里搜尋防空避難所
        /// GET: api/airraidshelter/village/{village}
        /// </summary>
        /// <param name="village">村里名稱（例如：大有里）</param>
        /// <returns>該村里的防空避難所列表</returns>
        [HttpGet("village/{village}")]
        public async Task<ActionResult<List<AirRaidShelter>>> GetSheltersByVillage(string village)
        {
            try
            {
                var shelters = await _shelterService.GetSheltersByVillageAsync(village);
                
                if (shelters.Count == 0)
                {
                    return NotFound(new { message = $"找不到村里 '{village}' 的防空避難所" });
                }

                return Ok(shelters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜尋村里 '{village}' 的防空避難所失敗");
                return StatusCode(500, new { error = "搜尋資料時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 搜尋附近的防空避難所
        /// GET: api/airraidshelter/nearby?latitude=25.060459&longitude=121.509074&radius=1.0
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <param name="longitude">經度</param>
        /// <param name="radius">搜尋半徑（公里），預設 5 公里</param>
        /// <returns>範圍內的防空避難所列表（依距離排序）</returns>
        [HttpGet("nearby")]
        public async Task<ActionResult<List<AirRaidShelter>>> GetNearbyShelters(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius = 5.0)
        {
            try
            {
                // 驗證輸入
                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { error = "緯度必須在 -90 到 90 之間" });
                }

                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { error = "經度必須在 -180 到 180 之間" });
                }

                if (radius <= 0 || radius > 100)
                {
                    return BadRequest(new { error = "半徑必須在 0 到 100 公里之間" });
                }

                var shelters = await _shelterService.GetNearbySheltersAsync(latitude, longitude, radius);
                
                return Ok(new
                {
                    searchLocation = new { latitude, longitude },
                    radiusInKm = radius,
                    count = shelters.Count,
                    shelters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋附近防空避難所失敗");
                return StatusCode(500, new { error = "搜尋資料時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 從自訂 URL 獲取防空避難所資料
        /// POST: api/airraidshelter/import
        /// </summary>
        /// <param name="request">包含 URL 的請求物件</param>
        /// <returns>解析後的防空避難所列表</returns>
        [HttpPost("import")]
        public async Task<ActionResult<List<AirRaidShelter>>> ImportFromUrl([FromBody] ImportRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Url))
                {
                    return BadRequest(new { error = "URL 不能為空" });
                }

                if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
                {
                    return BadRequest(new { error = "URL 格式不正確" });
                }

                var shelters = await _shelterService.FetchAndParseAirRaidSheltersAsync(request.Url);
                
                return Ok(new
                {
                    source = request.Url,
                    count = shelters.Count,
                    shelters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"從 URL {request.Url} 匯入資料失敗");
                return StatusCode(500, new { error = "匯入資料時發生錯誤", message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 匯入請求模型
    /// </summary>
    public class ImportRequest
    {
        public required string Url { get; set; }
    }
}
