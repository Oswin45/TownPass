using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// 地理編碼 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GeocodeController : ControllerBase
    {
        private readonly GoogleMapsService _googleMapsService;
        private readonly ILogger<GeocodeController> _logger;

        public GeocodeController(
            GoogleMapsService googleMapsService,
            ILogger<GeocodeController> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
        }

        /// <summary>
        /// 將地址轉換為地理座標
        /// </summary>
        /// <param name="request">地理編碼請求</param>
        /// <returns>包含座標的回應</returns>
        /// <response code="200">成功取得座標</response>
        /// <response code="400">請求參數錯誤</response>
        [HttpPost("geocode")]
        [ProducesResponseType(typeof(GeocodeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GeocodeResponse>> Geocode([FromBody] GeocodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = "Address is required"
                });
            }

            var result = await _googleMapsService.GeocodeAddressAsync(request);
            
            if (!result.Success)
            {
                return Ok(result); // 仍返回 200，但在 response 中標示失敗
            }

            return Ok(result);
        }

        /// <summary>
        /// 將地理座標轉換為地址
        /// </summary>
        /// <param name="request">反向地理編碼請求</param>
        /// <returns>包含地址的回應</returns>
        /// <response code="200">成功取得地址</response>
        /// <response code="400">請求參數錯誤</response>
        [HttpPost("reverse-geocode")]
        [ProducesResponseType(typeof(GeocodeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GeocodeResponse>> ReverseGeocode([FromBody] ReverseGeocodeRequest request)
        {
            if (request.Latitude < -90 || request.Latitude > 90)
            {
                return BadRequest(new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = "Latitude must be between -90 and 90"
                });
            }

            if (request.Longitude < -180 || request.Longitude > 180)
            {
                return BadRequest(new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = "Longitude must be between -180 and 180"
                });
            }

            var result = await _googleMapsService.ReverseGeocodeAsync(request);
            
            if (!result.Success)
            {
                return Ok(result); // 仍返回 200，但在 response 中標示失敗
            }

            return Ok(result);
        }

        /// <summary>
        /// 透過 GET 方式進行地理編碼 (簡化版)
        /// </summary>
        /// <param name="address">要查詢的地址</param>
        /// <param name="language">語言代碼 (預設: zh-TW)</param>
        /// <returns>包含座標的回應</returns>
        [HttpGet("geocode")]
        [ProducesResponseType(typeof(GeocodeResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GeocodeResponse>> GeocodeGet(
            [FromQuery] string address,
            [FromQuery] string? language = "zh-TW")
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest(new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = "Address parameter is required"
                });
            }

            var request = new GeocodeRequest
            {
                Address = address,
                Language = language
            };

            var result = await _googleMapsService.GeocodeAddressAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// 透過 GET 方式進行反向地理編碼 (簡化版)
        /// </summary>
        /// <param name="lat">緯度</param>
        /// <param name="lng">經度</param>
        /// <param name="language">語言代碼 (預設: zh-TW)</param>
        /// <returns>包含地址的回應</returns>
        [HttpGet("reverse-geocode")]
        [ProducesResponseType(typeof(GeocodeResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GeocodeResponse>> ReverseGeocodeGet(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] string? language = "zh-TW")
        {
            var request = new ReverseGeocodeRequest
            {
                Latitude = lat,
                Longitude = lng,
                Language = language
            };

            var result = await _googleMapsService.ReverseGeocodeAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// 批次地理編碼
        /// </summary>
        /// <param name="addresses">地址列表</param>
        /// <returns>地理編碼結果列表</returns>
        [HttpPost("batch-geocode")]
        [ProducesResponseType(typeof(List<GeocodeResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<GeocodeResponse>>> BatchGeocode([FromBody] List<string> addresses)
        {
            if (addresses == null || addresses.Count == 0)
            {
                return BadRequest("Address list cannot be empty");
            }

            if (addresses.Count > 50)
            {
                return BadRequest("Maximum 50 addresses allowed per batch request");
            }

            var results = await _googleMapsService.BatchGeocodeAsync(addresses);
            return Ok(results);
        }
    }
}
