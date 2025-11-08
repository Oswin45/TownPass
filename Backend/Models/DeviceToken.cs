using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// 儲存裝置的 FCM Token 以發送推播通知
    /// </summary>
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// FCM 裝置 Token
        /// </summary>
        [Required]
        [MaxLength(500)]
        public required string Token { get; set; }

        /// <summary>
        /// 裝置 ID（可選，用於識別裝置）
        /// </summary>
        [MaxLength(200)]
        public string? DeviceId { get; set; }

        /// <summary>
        /// 使用者 ID（可選，用於識別使用者）
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// 裝置平台（iOS, Android）
        /// </summary>
        [MaxLength(50)]
        public string? Platform { get; set; }

        /// <summary>
        /// Token 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Token 最後更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Token 是否啟用
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
