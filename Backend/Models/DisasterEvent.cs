using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class DisasterEvent
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Base64 encoded image string
        /// </summary>
        public required string Img { get; set; }
        
        /// <summary>
        /// Tags stored as comma-separated string in database
        /// </summary>
        public string TagsString { get; set; } = string.Empty;
        
        public required string Description { get; set; }
        public required string Title { get; set; }
        public required float Lnt { get; set; }
        public required float Lat { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Property for working with tags as array (not mapped to database)
        /// </summary>
        public string[] Tags
        {
            get => string.IsNullOrEmpty(TagsString) ? Array.Empty<string>() : TagsString.Split(',');
            set => TagsString = string.Join(',', value);
        }
    }
}
