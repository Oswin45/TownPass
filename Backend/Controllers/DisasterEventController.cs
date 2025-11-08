using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisasterEventController : ControllerBase
    {
        private readonly ILogger<DisasterEventController> _logger;
        private readonly ShelterDbContext _context;

        public DisasterEventController(
            ILogger<DisasterEventController> logger,
            ShelterDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 建立新的災害事件
        /// POST /api/DisasterEvent
        /// </summary>
        /// <param name="dto">災害事件資料</param>
        /// <returns>建立的災害事件（包含伺服器生成的 ID）</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDisasterEvent([FromBody] CreateDisasterEventDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "驗證失敗",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var disasterEvent = new DisasterEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Img = dto.Img,
                    Tags = dto.Tags,
                    Description = dto.Description,
                    Title = dto.Title,
                    Lnt = dto.Lnt,
                    Lat = dto.Lat,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DisasterEvents.Add(disasterEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"成功建立災害事件，ID: {disasterEvent.Id}");

                return CreatedAtAction(
                    nameof(GetDisasterEventById),
                    new { id = disasterEvent.Id },
                    new
                    {
                        success = true,
                        message = "災害事件建立成功",
                        data = new
                        {
                            id = disasterEvent.Id,
                            img = disasterEvent.Img,
                            tags = disasterEvent.Tags,
                            description = disasterEvent.Description,
                            title = disasterEvent.Title,
                            lnt = disasterEvent.Lnt,
                            lat = disasterEvent.Lat,
                            createdAt = disasterEvent.CreatedAt,
                            updatedAt = disasterEvent.UpdatedAt
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立災害事件時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "建立災害事件時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 取得所有災害事件
        /// GET /api/DisasterEvent
        /// </summary>
        /// <param name="skip">跳過的記錄數（用於分頁）</param>
        /// <param name="take">取得的記錄數（用於分頁）</param>
        /// <returns>災害事件列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDisasterEvents(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100)
        {
            try
            {
                if (take > 500)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "每次最多只能取得 500 筆記錄"
                    });
                }

                var totalCount = await _context.DisasterEvents.CountAsync();
                
                var events = await _context.DisasterEvents
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .Select(e => new
                    {
                        id = e.Id,
                        img = e.Img,
                        tags = e.Tags,
                        description = e.Description,
                        title = e.Title,
                        lnt = e.Lnt,
                        lat = e.Lat,
                        createdAt = e.CreatedAt,
                        updatedAt = e.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation($"取得 {events.Count} 個災害事件（總共 {totalCount} 個）");

                return Ok(new
                {
                    success = true,
                    totalCount,
                    count = events.Count,
                    skip,
                    take,
                    data = events
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得災害事件列表時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "取得災害事件列表時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 根據 ID 取得災害事件
        /// GET /api/DisasterEvent/{id}
        /// </summary>
        /// <param name="id">災害事件 ID</param>
        /// <returns>災害事件詳細資料</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDisasterEventById(string id)
        {
            try
            {
                var disasterEvent = await _context.DisasterEvents
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (disasterEvent == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"找不到 ID 為 {id} 的災害事件"
                    });
                }

                _logger.LogInformation($"取得災害事件，ID: {id}");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = disasterEvent.Id,
                        img = disasterEvent.Img,
                        tags = disasterEvent.Tags,
                        description = disasterEvent.Description,
                        title = disasterEvent.Title,
                        lnt = disasterEvent.Lnt,
                        lat = disasterEvent.Lat,
                        createdAt = disasterEvent.CreatedAt,
                        updatedAt = disasterEvent.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取得災害事件（ID: {id}）時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "取得災害事件時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 更新災害事件
        /// PUT /api/DisasterEvent/{id}
        /// </summary>
        /// <param name="id">災害事件 ID</param>
        /// <param name="dto">更新的資料</param>
        /// <returns>更新後的災害事件</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDisasterEvent(
            string id,
            [FromBody] UpdateDisasterEventDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "驗證失敗",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var disasterEvent = await _context.DisasterEvents
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (disasterEvent == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"找不到 ID 為 {id} 的災害事件"
                    });
                }

                // 更新非空欄位
                if (dto.Img != null)
                    disasterEvent.Img = dto.Img;
                
                if (dto.Tags != null)
                    disasterEvent.Tags = dto.Tags;
                
                if (dto.Description != null)
                    disasterEvent.Description = dto.Description;
                
                if (dto.Title != null)
                    disasterEvent.Title = dto.Title;
                
                if (dto.Lnt.HasValue)
                    disasterEvent.Lnt = dto.Lnt.Value;
                
                if (dto.Lat.HasValue)
                    disasterEvent.Lat = dto.Lat.Value;

                disasterEvent.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"成功更新災害事件，ID: {id}");

                return Ok(new
                {
                    success = true,
                    message = "災害事件更新成功",
                    data = new
                    {
                        id = disasterEvent.Id,
                        img = disasterEvent.Img,
                        tags = disasterEvent.Tags,
                        description = disasterEvent.Description,
                        title = disasterEvent.Title,
                        lnt = disasterEvent.Lnt,
                        lat = disasterEvent.Lat,
                        createdAt = disasterEvent.CreatedAt,
                        updatedAt = disasterEvent.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新災害事件（ID: {id}）時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "更新災害事件時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 刪除災害事件
        /// DELETE /api/DisasterEvent/{id}
        /// </summary>
        /// <param name="id">災害事件 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDisasterEvent(string id)
        {
            try
            {
                var disasterEvent = await _context.DisasterEvents
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (disasterEvent == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"找不到 ID 為 {id} 的災害事件"
                    });
                }

                _context.DisasterEvents.Remove(disasterEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"成功刪除災害事件，ID: {id}");

                return Ok(new
                {
                    success = true,
                    message = "災害事件刪除成功",
                    deletedId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"刪除災害事件（ID: {id}）時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "刪除災害事件時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 根據座標範圍搜尋災害事件
        /// GET /api/DisasterEvent/nearby?lat=25.0330&lnt=121.5654&radius=5
        /// </summary>
        /// <param name="lat">緯度</param>
        /// <param name="lnt">經度</param>
        /// <param name="radius">半徑（公里）</param>
        /// <returns>附近的災害事件列表</returns>
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyDisasterEvents(
            [FromQuery] float lat,
            [FromQuery] float lnt,
            [FromQuery] float radius = 5)
        {
            try
            {
                if (radius <= 0 || radius > 100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "半徑必須在 0 到 100 公里之間"
                    });
                }

                // 簡單的邊界框篩選（經緯度大約 1 度 = 111 公里）
                var latDelta = radius / 111.0f;
                var lntDelta = radius / (111.0f * (float)Math.Cos(lat * Math.PI / 180.0));

                var events = await _context.DisasterEvents
                    .Where(e => e.Lat >= lat - latDelta && e.Lat <= lat + latDelta &&
                               e.Lnt >= lnt - lntDelta && e.Lnt <= lnt + lntDelta)
                    .Select(e => new
                    {
                        id = e.Id,
                        img = e.Img,
                        tags = e.Tags,
                        description = e.Description,
                        title = e.Title,
                        lnt = e.Lnt,
                        lat = e.Lat,
                        createdAt = e.CreatedAt,
                        updatedAt = e.UpdatedAt,
                        // 計算大約距離（公里）
                        distance = Math.Sqrt(
                            Math.Pow((e.Lat - lat) * 111.0, 2) +
                            Math.Pow((e.Lnt - lnt) * 111.0 * Math.Cos(lat * Math.PI / 180.0), 2)
                        )
                    })
                    .Where(e => e.distance <= radius)
                    .OrderBy(e => e.distance)
                    .ToListAsync();

                _logger.LogInformation($"在 ({lat}, {lnt}) 附近 {radius} 公里內找到 {events.Count} 個災害事件");

                return Ok(new
                {
                    success = true,
                    count = events.Count,
                    center = new { lat, lnt },
                    radius,
                    data = events
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋附近災害事件時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "搜尋附近災害事件時發生錯誤",
                    error = ex.Message
                });
            }
        }
    }
}
