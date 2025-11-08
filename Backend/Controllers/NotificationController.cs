using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    /// <summary>
    /// 推播通知控制器
    /// 處理裝置註冊與災害通知
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly ShelterDbContext _context;
        private readonly FcmNotificationService _fcmService;

        public NotificationController(
            ILogger<NotificationController> logger,
            ShelterDbContext context,
            FcmNotificationService fcmService)
        {
            _logger = logger;
            _context = context;
            _fcmService = fcmService;
        }

        /// <summary>
        /// 註冊裝置 Token
        /// POST /api/Notification/RegisterDevice
        /// </summary>
        [HttpPost("RegisterDevice")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceTokenDto dto)
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

                // 檢查 Token 是否已存在
                var existingToken = await _context.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.Token == dto.Token);

                if (existingToken != null)
                {
                    // 更新現有 Token
                    existingToken.DeviceId = dto.DeviceId ?? existingToken.DeviceId;
                    existingToken.UserId = dto.UserId ?? existingToken.UserId;
                    existingToken.Platform = dto.Platform ?? existingToken.Platform;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    existingToken.IsActive = true;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"更新裝置 Token: {dto.Token.Substring(0, 20)}...");

                    return Ok(new
                    {
                        success = true,
                        message = "裝置 Token 更新成功",
                        data = new
                        {
                            id = existingToken.Id,
                            isNew = false
                        }
                    });
                }

                // 建立新的 Token
                var deviceToken = new DeviceToken
                {
                    Token = dto.Token,
                    DeviceId = dto.DeviceId,
                    UserId = dto.UserId,
                    Platform = dto.Platform,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.DeviceTokens.Add(deviceToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"註冊新裝置 Token: {dto.Token.Substring(0, 20)}...");

                return Ok(new
                {
                    success = true,
                    message = "裝置註冊成功",
                    data = new
                    {
                        id = deviceToken.Id,
                        isNew = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "註冊裝置時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "註冊裝置時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 取消註冊裝置
        /// DELETE /api/Notification/UnregisterDevice
        /// </summary>
        [HttpDelete("UnregisterDevice")]
        public async Task<IActionResult> UnregisterDevice([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Token 不能為空"
                    });
                }

                var deviceToken = await _context.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.Token == token);

                if (deviceToken == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "找不到該裝置 Token"
                    });
                }

                deviceToken.IsActive = false;
                deviceToken.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"取消註冊裝置 Token: {token.Substring(0, 20)}...");

                return Ok(new
                {
                    success = true,
                    message = "裝置取消註冊成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消註冊裝置時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "取消註冊裝置時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 觸發災害事件並發送推播通知
        /// POST /api/Notification/TriggerDisaster
        /// </summary>
        [HttpPost("TriggerDisaster")]
        public async Task<IActionResult> TriggerDisaster([FromBody] TriggerDisasterDto dto)
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

                // 建立災害事件
                var disasterEvent = new DisasterEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = dto.Title,
                    Description = dto.Description,
                    Lnt = dto.Longitude,
                    Lat = dto.Latitude,
                    Tags = dto.Tags ?? new[] { "emergency" },
                    Img = dto.ImageBase64 ?? "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DisasterEvents.Add(disasterEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    $"觸發災害事件: {disasterEvent.Title} 於座標 ({disasterEvent.Lat}, {disasterEvent.Lnt})");

                int notificationsSent = 0;

                // 發送推播通知
                if (dto.SendNotification)
                {
                    notificationsSent = await _fcmService.SendDisasterNotificationAsync(
                        disasterEvent, 
                        dto.NotificationRadiusKm);

                    _logger.LogInformation($"已發送 {notificationsSent} 則推播通知");
                }

                return Ok(new
                {
                    success = true,
                    message = "災害事件觸發成功",
                    data = new
                    {
                        disasterId = disasterEvent.Id,
                        title = disasterEvent.Title,
                        description = disasterEvent.Description,
                        latitude = disasterEvent.Lat,
                        longitude = disasterEvent.Lnt,
                        tags = disasterEvent.Tags,
                        createdAt = disasterEvent.CreatedAt,
                        notificationsSent = notificationsSent
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "觸發災害事件時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "觸發災害事件時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 取得所有已註冊的裝置（管理用）
        /// GET /api/Notification/RegisteredDevices
        /// </summary>
        [HttpGet("RegisteredDevices")]
        public async Task<IActionResult> GetRegisteredDevices([FromQuery] bool? activeOnly = true)
        {
            try
            {
                var query = _context.DeviceTokens.AsQueryable();

                if (activeOnly == true)
                {
                    query = query.Where(dt => dt.IsActive);
                }

                var devices = await query
                    .OrderByDescending(dt => dt.UpdatedAt)
                    .Select(dt => new
                    {
                        id = dt.Id,
                        deviceId = dt.DeviceId,
                        userId = dt.UserId,
                        platform = dt.Platform,
                        isActive = dt.IsActive,
                        createdAt = dt.CreatedAt,
                        updatedAt = dt.UpdatedAt,
                        tokenPreview = dt.Token.Substring(0, Math.Min(20, dt.Token.Length)) + "..."
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "取得裝置清單成功",
                    data = devices,
                    count = devices.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得裝置清單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "取得裝置清單時發生錯誤",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 測試推播通知
        /// POST /api/Notification/TestNotification
        /// </summary>
        [HttpPost("TestNotification")]
        public async Task<IActionResult> TestNotification([FromQuery] string? token = null)
        {
            try
            {
                string targetToken;

                if (!string.IsNullOrWhiteSpace(token))
                {
                    targetToken = token;
                }
                else
                {
                    // 如果沒有指定 Token，使用第一個啟用的裝置
                    var firstDevice = await _context.DeviceTokens
                        .Where(dt => dt.IsActive)
                        .FirstOrDefaultAsync();

                    if (firstDevice == null)
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "沒有可用的裝置 Token"
                        });
                    }

                    targetToken = firstDevice.Token;
                }

                var result = await _fcmService.SendNotificationToDeviceAsync(
                    targetToken,
                    "測試通知",
                    "這是一則測試推播通知",
                    new Dictionary<string, string>
                    {
                        { "type", "test" },
                        { "timestamp", DateTime.UtcNow.ToString("o") }
                    });

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "測試通知發送成功"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "測試通知發送失敗"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送測試通知時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = "發送測試通知時發生錯誤",
                    error = ex.Message
                });
            }
        }
    }
}
