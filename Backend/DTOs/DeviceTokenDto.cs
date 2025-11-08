using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// 註冊裝置 Token 的 DTO
    /// </summary>
    public class RegisterDeviceTokenDto
    {
        [Required(ErrorMessage = "Token 為必填欄位")]
        public required string Token { get; set; }

        public string? DeviceId { get; set; }
        
        public string? UserId { get; set; }
        
        public string? Platform { get; set; }
    }

    /// <summary>
    /// 觸發災害事件的 DTO
    /// </summary>
    public class TriggerDisasterDto
    {
        [Required(ErrorMessage = "標題為必填欄位")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "描述為必填欄位")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "經度為必填欄位")]
        public required float Longitude { get; set; }

        [Required(ErrorMessage = "緯度為必填欄位")]
        public required float Latitude { get; set; }

        public string[]? Tags { get; set; }

        public string? ImageBase64 { get; set; }

        /// <summary>
        /// 是否發送推播通知（預設為 true）
        /// </summary>
        public bool SendNotification { get; set; } = true;

        /// <summary>
        /// 通知半徑（公里），null 表示通知所有裝置
        /// </summary>
        public double? NotificationRadiusKm { get; set; }
    }
}
