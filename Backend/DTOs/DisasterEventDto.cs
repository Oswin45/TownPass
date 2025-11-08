using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// DTO for creating or updating a disaster event
    /// </summary>
    public class CreateDisasterEventDto
    {
        /// <summary>
        /// Base64 encoded image string
        /// </summary>
        [Required(ErrorMessage = "圖片必須提供")]
        public required string Img { get; set; }
        
        /// <summary>
        /// Event tags
        /// </summary>
        [Required(ErrorMessage = "標籤必須提供")]
        public required string[] Tags { get; set; }
        
        /// <summary>
        /// Event description
        /// </summary>
        [Required(ErrorMessage = "描述必須提供")]
        [StringLength(2000, ErrorMessage = "描述長度不能超過2000字元")]
        public required string Description { get; set; }
        
        /// <summary>
        /// Event title
        /// </summary>
        [Required(ErrorMessage = "標題必須提供")]
        [StringLength(200, ErrorMessage = "標題長度不能超過200字元")]
        public required string Title { get; set; }
        
        /// <summary>
        /// Longitude
        /// </summary>
        [Required(ErrorMessage = "經度必須提供")]
        [Range(-180, 180, ErrorMessage = "經度必須在-180到180之間")]
        public required float Lnt { get; set; }
        
        /// <summary>
        /// Latitude
        /// </summary>
        [Required(ErrorMessage = "緯度必須提供")]
        [Range(-90, 90, ErrorMessage = "緯度必須在-90到90之間")]
        public required float Lat { get; set; }
    }
    
    /// <summary>
    /// DTO for updating a disaster event
    /// </summary>
    public class UpdateDisasterEventDto
    {
        /// <summary>
        /// Base64 encoded image string
        /// </summary>
        public string? Img { get; set; }
        
        /// <summary>
        /// Event tags
        /// </summary>
        public string[]? Tags { get; set; }
        
        /// <summary>
        /// Event description
        /// </summary>
        [StringLength(2000, ErrorMessage = "描述長度不能超過2000字元")]
        public string? Description { get; set; }
        
        /// <summary>
        /// Event title
        /// </summary>
        [StringLength(200, ErrorMessage = "標題長度不能超過200字元")]
        public string? Title { get; set; }
        
        /// <summary>
        /// Longitude
        /// </summary>
        [Range(-180, 180, ErrorMessage = "經度必須在-180到180之間")]
        public float? Lnt { get; set; }
        
        /// <summary>
        /// Latitude
        /// </summary>
        [Range(-90, 90, ErrorMessage = "緯度必須在-90到90之間")]
        public float? Lat { get; set; }
    }
}
