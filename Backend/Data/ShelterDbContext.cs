using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    /// <summary>
    /// SQLite 資料庫上下文，用於快取避難所資料
    /// </summary>
    public class ShelterDbContext : DbContext
    {
        public ShelterDbContext(DbContextOptions<ShelterDbContext> options)
            : base(options)
        {
        }

        public DbSet<Shelter> Shelters { get; set; } = null!;
        public DbSet<CacheMetadata> CacheMetadata { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 設定 Shelter 表
            modelBuilder.Entity<Shelter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Type).HasMaxLength(100);
                entity.Property(e => e.Telephone).HasMaxLength(50);
                
                // 建立索引以提高查詢效能
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Address);
                entity.HasIndex(e => e.SupportedDisasters);
                entity.HasIndex(e => new { e.Latitude, e.Longitude });
            });

            // 設定 CacheMetadata 表
            modelBuilder.Entity<CacheMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CacheKey).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.CacheKey).IsUnique();
            });
        }
    }

    /// <summary>
    /// 快取元數據，記錄快取的建立和更新時間
    /// </summary>
    public class CacheMetadata
    {
        public int Id { get; set; }
        public required string CacheKey { get; set; }
        public DateTime LastUpdated { get; set; }
        public int RecordCount { get; set; }
        public string? Notes { get; set; }
    }
}
