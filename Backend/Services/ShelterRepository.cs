using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    /// <summary>
    /// 避難所資料儲存庫服務，處理資料庫的 CRUD 操作
    /// </summary>
    public class ShelterRepository
    {
        private readonly ShelterDbContext _context;
        private readonly ILogger<ShelterRepository> _logger;
        private const string CACHE_KEY = "AllShelters";

        public ShelterRepository(ShelterDbContext context, ILogger<ShelterRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 獲取所有快取的避難所
        /// </summary>
        public async Task<List<Shelter>> GetAllSheltersAsync()
        {
            return await _context.Shelters.ToListAsync();
        }

        /// <summary>
        /// 根據災害類型獲取避難所
        /// </summary>
        public async Task<List<Shelter>> GetSheltersByDisasterTypeAsync(DisasterTypes disasterType)
        {
            return await _context.Shelters
                .Where(s => (s.SupportedDisasters & disasterType) != 0)
                .ToListAsync();
        }

        /// <summary>
        /// 根據名稱搜尋避難所
        /// </summary>
        public async Task<List<Shelter>> SearchSheltersByNameAsync(string name)
        {
            return await _context.Shelters
                .Where(s => EF.Functions.Like(s.Name, $"%{name}%"))
                .ToListAsync();
        }

        /// <summary>
        /// 根據地址搜尋避難所
        /// </summary>
        public async Task<List<Shelter>> SearchSheltersByAddressAsync(string address)
        {
            return await _context.Shelters
                .Where(s => EF.Functions.Like(s.Address, $"%{address}%"))
                .ToListAsync();
        }

        /// <summary>
        /// 獲取附近的避難所
        /// </summary>
        public async Task<List<Shelter>> GetNearbySheltersAsync(double latitude, double longitude, double radiusInKm)
        {
            // 簡單的邊界框過濾（在資料庫層級）
            // 1度緯度 ≈ 111 km, 1度經度在台灣 ≈ 96 km
            var latDelta = radiusInKm / 111.0;
            var lonDelta = radiusInKm / 96.0;

            var minLat = latitude - latDelta;
            var maxLat = latitude + latDelta;
            var minLon = longitude - lonDelta;
            var maxLon = longitude + lonDelta;

            var shelters = await _context.Shelters
                .Where(s => s.Latitude >= minLat && s.Latitude <= maxLat &&
                           s.Longitude >= minLon && s.Longitude <= maxLon)
                .ToListAsync();

            // 在記憶體中精確計算距離並過濾
            return shelters
                .Where(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude) <= radiusInKm)
                .OrderBy(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude))
                .ToList();
        }

        /// <summary>
        /// 獲取有無障礙設施的避難所
        /// </summary>
        public async Task<List<Shelter>> GetAccessibleSheltersAsync()
        {
            return await _context.Shelters
                .Where(s => s.Accesibility)
                .ToListAsync();
        }

        /// <summary>
        /// 獲取容量大於指定值的避難所
        /// </summary>
        public async Task<List<Shelter>> GetSheltersByMinCapacityAsync(int minCapacity)
        {
            return await _context.Shelters
                .Where(s => s.Capacity >= minCapacity)
                .OrderByDescending(s => s.Capacity)
                .ToListAsync();
        }

        /// <summary>
        /// 清空資料庫並插入新的避難所資料
        /// </summary>
        public async Task RefreshAllSheltersAsync(List<Shelter> shelters)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 清空現有資料
                _context.Shelters.RemoveRange(_context.Shelters);
                await _context.SaveChangesAsync();

                // 重置 ID（對於 SQLite）
                // 注意：這會重新開始 ID 序列
                foreach (var shelter in shelters)
                {
                    shelter.Id = 0; // 讓 EF Core 自動分配新 ID
                }

                // 插入新資料
                await _context.Shelters.AddRangeAsync(shelters);
                await _context.SaveChangesAsync();

                // 更新或新增快取元數據
                var metadata = await _context.CacheMetadata
                    .FirstOrDefaultAsync(m => m.CacheKey == CACHE_KEY);

                if (metadata == null)
                {
                    metadata = new CacheMetadata
                    {
                        CacheKey = CACHE_KEY,
                        LastUpdated = DateTime.UtcNow,
                        RecordCount = shelters.Count,
                        Notes = "Initial cache creation"
                    };
                    await _context.CacheMetadata.AddAsync(metadata);
                }
                else
                {
                    metadata.LastUpdated = DateTime.UtcNow;
                    metadata.RecordCount = shelters.Count;
                    metadata.Notes = "Cache refreshed";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"成功更新快取，共 {shelters.Count} 筆避難所資料");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "更新避難所快取時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 獲取快取元數據
        /// </summary>
        public async Task<CacheMetadata?> GetCacheMetadataAsync()
        {
            return await _context.CacheMetadata
                .FirstOrDefaultAsync(m => m.CacheKey == CACHE_KEY);
        }

        /// <summary>
        /// 檢查快取是否存在
        /// </summary>
        public async Task<bool> HasCacheAsync()
        {
            return await _context.Shelters.AnyAsync();
        }

        /// <summary>
        /// 獲取快取中的避難所總數
        /// </summary>
        public async Task<int> GetCachedShelterCountAsync()
        {
            return await _context.Shelters.CountAsync();
        }

        /// <summary>
        /// 計算兩點間的距離（Haversine 公式）
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // 地球半徑（公里）

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
