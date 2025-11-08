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
        /// 將 NaturalDisasterShelter 轉換為 Shelter
        /// </summary>
        private Shelter ConvertToShelter(NaturalDisasterShelter source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // 解析災害類型
            var supportedDisasters = DisasterTypes.None; // 預設支援空襲

            if (!string.IsNullOrEmpty(source.FloodDisaster) && (source.FloodDisaster == "Y" || source.FloodDisaster == "備用"))
                supportedDisasters |= DisasterTypes.Flooding;

            if (!string.IsNullOrEmpty(source.EarthquakeDisaster) && (source.EarthquakeDisaster == "Y" || source.EarthquakeDisaster == "備用"))
                supportedDisasters |= DisasterTypes.Earthquake;

            if (!string.IsNullOrEmpty(source.Landslide) && (source.Landslide == "Y" || source.Landslide == "備用"))
                supportedDisasters |= DisasterTypes.Landslide;

            if (!string.IsNullOrEmpty(source.Tsunami) && (source.Tsunami == "是" || source.Tsunami == "備用"))
                supportedDisasters |= DisasterTypes.Tsunami;

            // 解析容納人數
            int.TryParse(source.Capacity?.Trim(), out int capacity);

            // 解析面積
            int.TryParse(source.Area?.Trim(), out int area);

            // 解析無障礙設施
            bool hasAccessibility = !string.IsNullOrEmpty(source.AccessibleFacilities) && source.AccessibleFacilities == "是";

            return new Shelter
            {
                Type = source.Type,
                Name = source.Name ?? "未命名收容所",
                Capacity = capacity,
                SupportedDisasters = supportedDisasters,
                Accesibility = hasAccessibility,
                Address = source.Address ?? "",
                Latitude = 0, // 需要從地址進行地理編碼或從其他來源取得
                Longitude = 0, // 需要從地址進行地理編碼或從其他來源取得
                Telephone = source.ContactPersonPhone ?? source.ManagerPhone,
                SizeInSquareMeters = area
            };
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllShelters()
        {
            try
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

                return Ok(shelterResponse.Result.Results.Select(x => ConvertToShelter(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取避難收容所資料時發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
