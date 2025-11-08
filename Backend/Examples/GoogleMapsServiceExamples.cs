using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Examples
{
    /// <summary>
    /// Google Maps Service 使用範例
    /// 這個類別展示如何使用 GoogleMapsService
    /// </summary>
    public class GoogleMapsServiceExamples
    {
        private readonly GoogleMapsService _googleMapsService;

        public GoogleMapsServiceExamples(GoogleMapsService googleMapsService)
        {
            _googleMapsService = googleMapsService;
        }

        /// <summary>
        /// 範例 1: 基本地理編碼
        /// 將台北101的地址轉換為座標
        /// </summary>
        public async Task<GeocodeResponse> Example1_BasicGeocode()
        {
            var request = new GeocodeRequest
            {
                Address = "台北市信義區信義路五段7號",  // 台北101
                Language = "zh-TW",
                Region = "TW"
            };

            var response = await _googleMapsService.GeocodeAddressAsync(request);

            if (response.Success && response.Result != null)
            {
                Console.WriteLine($"地址: {response.Result.FormattedAddress}");
                Console.WriteLine($"緯度: {response.Result.Latitude}");
                Console.WriteLine($"經度: {response.Result.Longitude}");
                Console.WriteLine($"Place ID: {response.Result.PlaceId}");
            }
            else
            {
                Console.WriteLine($"錯誤: {response.ErrorMessage}");
            }

            return response;
        }

        /// <summary>
        /// 範例 2: 反向地理編碼
        /// 將座標轉換為地址
        /// </summary>
        public async Task<GeocodeResponse> Example2_ReverseGeocode()
        {
            var request = new ReverseGeocodeRequest
            {
                Latitude = 25.0339639,   // 台北101的座標
                Longitude = 121.5644722,
                Language = "zh-TW"
            };

            var response = await _googleMapsService.ReverseGeocodeAsync(request);

            if (response.Success && response.Result != null)
            {
                Console.WriteLine($"座標 ({request.Latitude}, {request.Longitude}) 的地址:");
                Console.WriteLine($"完整地址: {response.Result.FormattedAddress}");
                
                // 顯示地址組成部分
                foreach (var component in response.Result.AddressComponents)
                {
                    Console.WriteLine($"- {component.LongName} ({string.Join(", ", component.Types)})");
                }
            }
            else
            {
                Console.WriteLine($"錯誤: {response.ErrorMessage}");
            }

            return response;
        }

        /// <summary>
        /// 範例 3: 批次地理編碼
        /// 一次處理多個台北市的地標
        /// </summary>
        public async Task<List<GeocodeResponse>> Example3_BatchGeocode()
        {
            var addresses = new List<string>
            {
                "台北市中正區重慶南路一段122號",  // 中正紀念堂
                "台北市士林區福林路60號",         // 故宮博物院
                "台北市大安區羅斯福路四段1號",    // 台灣大學
                "台北市信義區市府路1號",          // 台北市政府
                "台北市萬華區康定路173號"         // 龍山寺
            };

            var results = await _googleMapsService.BatchGeocodeAsync(addresses);

            Console.WriteLine($"批次處理 {addresses.Count} 個地址:");
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Success && results[i].Result != null)
                {
                    Console.WriteLine($"{i + 1}. {addresses[i]}");
                    Console.WriteLine($"   座標: ({results[i].Result.Latitude}, {results[i].Result.Longitude})");
                }
                else
                {
                    Console.WriteLine($"{i + 1}. {addresses[i]} - 失敗: {results[i].ErrorMessage}");
                }
            }

            return results;
        }

        /// <summary>
        /// 範例 4: 更新避難所座標
        /// 自動為避難所取得正確的經緯度
        /// </summary>
        public async Task<bool> Example4_UpdateShelterCoordinates()
        {
            var shelter = new Shelter
            {
                Name = "台北市立大學",
                Address = "台北市中正區愛國西路1號",
                Type = "學校",
                Capacity = 1000,
                CurrentOccupancy = 0,
                SupportedDisasters = DisasterTypes.Earthquake | DisasterTypes.Flooding,
                Accesibility = true,
                SizeInSquareMeters = 5000,
                Latitude = 0,  // 尚未設定
                Longitude = 0  // 尚未設定
            };

            Console.WriteLine($"避難所: {shelter.Name}");
            Console.WriteLine($"地址: {shelter.Address}");
            Console.WriteLine($"更新前座標: ({shelter.Latitude}, {shelter.Longitude})");

            var success = await _googleMapsService.UpdateShelterCoordinatesAsync(shelter);

            if (success)
            {
                Console.WriteLine($"更新後座標: ({shelter.Latitude}, {shelter.Longitude})");
                Console.WriteLine("✓ 座標更新成功！");
            }
            else
            {
                Console.WriteLine("✗ 座標更新失敗");
            }

            return success;
        }

        /// <summary>
        /// 範例 5: 處理不同的地址格式
        /// 測試服務對各種地址格式的處理能力
        /// </summary>
        public async Task Example5_DifferentAddressFormats()
        {
            var testAddresses = new List<string>
            {
                "台北101",                           // 地標名稱
                "台北市信義區信義路五段7號",        // 完整地址
                "110台北市信義區信義路五段7號",     // 包含郵遞區號
                "Taipei 101",                        // 英文名稱
                "No. 7, Section 5, Xinyi Road, Xinyi District, Taipei City" // 英文完整地址
            };

            Console.WriteLine("測試不同的地址格式:");
            foreach (var address in testAddresses)
            {
                var request = new GeocodeRequest { Address = address };
                var response = await _googleMapsService.GeocodeAddressAsync(request);

                Console.WriteLine($"\n輸入: {address}");
                if (response.Success && response.Result != null)
                {
                    Console.WriteLine($"✓ 成功: {response.Result.FormattedAddress}");
                    Console.WriteLine($"  座標: ({response.Result.Latitude}, {response.Result.Longitude})");
                }
                else
                {
                    Console.WriteLine($"✗ 失敗: {response.ErrorMessage}");
                }
            }
        }

        /// <summary>
        /// 範例 6: 錯誤處理
        /// 展示如何處理各種錯誤情況
        /// </summary>
        public async Task Example6_ErrorHandling()
        {
            Console.WriteLine("測試錯誤處理:");

            // 測試 1: 空地址
            var emptyAddressRequest = new GeocodeRequest { Address = "" };
            var response1 = await _googleMapsService.GeocodeAddressAsync(emptyAddressRequest);
            Console.WriteLine($"空地址: {(response1.Success ? "成功" : $"失敗 - {response1.ErrorMessage}")}");

            // 測試 2: 不存在的地址
            var invalidAddressRequest = new GeocodeRequest { Address = "這是一個不存在的地址12345XYZ" };
            var response2 = await _googleMapsService.GeocodeAddressAsync(invalidAddressRequest);
            Console.WriteLine($"無效地址: {(response2.Success ? "成功" : $"失敗 - {response2.ErrorMessage}")}");

            // 測試 3: 無效的座標
            var invalidCoordRequest = new ReverseGeocodeRequest 
            { 
                Latitude = 999,  // 超出範圍
                Longitude = 999 
            };
            var response3 = await _googleMapsService.ReverseGeocodeAsync(invalidCoordRequest);
            Console.WriteLine($"無效座標: {(response3.Success ? "成功" : $"失敗 - {response3.ErrorMessage}")}");
        }

        /// <summary>
        /// 範例 7: 計算距離
        /// 使用地理編碼結果計算兩個地點之間的距離
        /// </summary>
        public async Task Example7_CalculateDistance()
        {
            // 取得台北101的座標
            var taipei101 = await _googleMapsService.GeocodeAddressAsync(
                new GeocodeRequest { Address = "台北101" });

            // 取得台北車站的座標
            var taipeiStation = await _googleMapsService.GeocodeAddressAsync(
                new GeocodeRequest { Address = "台北車站" });

            if (taipei101.Success && taipeiStation.Success && 
                taipei101.Result != null && taipeiStation.Result != null)
            {
                var distance = CalculateHaversineDistance(
                    taipei101.Result.Latitude, taipei101.Result.Longitude,
                    taipeiStation.Result.Latitude, taipeiStation.Result.Longitude
                );

                Console.WriteLine($"台北101: ({taipei101.Result.Latitude}, {taipei101.Result.Longitude})");
                Console.WriteLine($"台北車站: ({taipeiStation.Result.Latitude}, {taipeiStation.Result.Longitude})");
                Console.WriteLine($"直線距離: {distance:F2} 公里");
            }
        }

        /// <summary>
        /// 使用 Haversine 公式計算兩點之間的距離
        /// </summary>
        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // 地球半徑（公里）
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
