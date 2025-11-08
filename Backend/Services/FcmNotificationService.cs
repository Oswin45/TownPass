using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Backend.Models;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    /// <summary>
    /// Firebase Cloud Messaging 推播通知服務
    /// </summary>
    public class FcmNotificationService
    {
        private readonly ILogger<FcmNotificationService> _logger;
        private readonly ShelterDbContext _context;
        private static bool _firebaseInitialized = false;
        private static readonly object _lock = new object();

        public FcmNotificationService(
            ILogger<FcmNotificationService> logger,
            ShelterDbContext context)
        {
            _logger = logger;
            _context = context;
            InitializeFirebase();
        }

        /// <summary>
        /// 初始化 Firebase Admin SDK
        /// </summary>
        private void InitializeFirebase()
        {
            if (_firebaseInitialized) return;

            lock (_lock)
            {
                if (_firebaseInitialized) return;

                try
                {
                    var credentialPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google-services.json");
                    
                    if (!File.Exists(credentialPath))
                    {
                        _logger.LogError($"找不到 Firebase 憑證檔案: {credentialPath}");
                        return;
                    }

                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });

                    _firebaseInitialized = true;
                    _logger.LogInformation("Firebase Admin SDK 初始化成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "初始化 Firebase Admin SDK 失敗");
                }
            }
        }

        /// <summary>
        /// 發送災害通知給所有已註冊的裝置
        /// </summary>
        /// <param name="disasterEvent">災害事件</param>
        /// <param name="radiusKm">通知半徑（公里），null 表示通知所有裝置</param>
        /// <returns>成功發送的通知數量</returns>
        public async Task<int> SendDisasterNotificationAsync(DisasterEvent disasterEvent, double? radiusKm = null)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogWarning("Firebase 尚未初始化，無法發送通知");
                return 0;
            }

            try
            {
                // 取得所有啟用的裝置 Token
                var activeTokens = await _context.DeviceTokens
                    .Where(dt => dt.IsActive)
                    .Select(dt => dt.Token)
                    .ToListAsync();

                if (!activeTokens.Any())
                {
                    _logger.LogWarning("沒有可用的裝置 Token");
                    return 0;
                }

                // 建立通知訊息
                var message = new MulticastMessage()
                {
                    Tokens = activeTokens,
                    Notification = new Notification()
                    {
                        Title = $"⚠️ {disasterEvent.Title}",
                        Body = disasterEvent.Description,
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "disasterId", disasterEvent.Id },
                        { "latitude", disasterEvent.Lat.ToString() },
                        { "longitude", disasterEvent.Lnt.ToString() },
                        { "tags", string.Join(",", disasterEvent.Tags) },
                        { "type", "disaster_alert" }
                    },
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification()
                        {
                            Sound = "default",
                            ChannelId = "disaster_alerts"
                        }
                    },
                    Apns = new ApnsConfig()
                    {
                        Aps = new Aps()
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    }
                };

                // 發送通知
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                _logger.LogInformation(
                    $"成功發送 {response.SuccessCount} 則通知，失敗 {response.FailureCount} 則");

                // 處理失敗的 Token（可能已過期或無效）
                if (response.FailureCount > 0)
                {
                    var failedTokens = new List<string>();
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            failedTokens.Add(activeTokens[i]);
                            _logger.LogWarning(
                                $"發送失敗: {response.Responses[i].Exception?.Message}, Token: {activeTokens[i]}");
                        }
                    }

                    // 將失敗的 Token 標記為非啟用
                    await DeactivateTokensAsync(failedTokens);
                }

                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送災害通知時發生錯誤");
                return 0;
            }
        }

        /// <summary>
        /// 發送通知給特定裝置
        /// </summary>
        public async Task<bool> SendNotificationToDeviceAsync(string token, string title, string body, Dictionary<string, string>? data = null)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogWarning("Firebase 尚未初始化，無法發送通知");
                return false;
            }

            try
            {
                var message = new Message()
                {
                    Token = token,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body,
                    },
                    Data = data,
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                    },
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"成功發送通知，訊息 ID: {response}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送通知失敗");
                return false;
            }
        }

        /// <summary>
        /// 停用無效的 Token
        /// </summary>
        private async Task DeactivateTokensAsync(List<string> tokens)
        {
            try
            {
                var deviceTokens = await _context.DeviceTokens
                    .Where(dt => tokens.Contains(dt.Token))
                    .ToListAsync();

                foreach (var dt in deviceTokens)
                {
                    dt.IsActive = false;
                    dt.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"已停用 {deviceTokens.Count} 個無效的裝置 Token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停用裝置 Token 時發生錯誤");
            }
        }
    }
}
