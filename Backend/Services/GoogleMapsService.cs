using Backend.Models;
using System.Text.Json;
using System.Web;

namespace Backend.Services
{
    /// <summary>
    /// Google Maps 地理編碼服務
    /// 提供地址轉換為座標 (Geocoding) 和座標轉換為地址 (Reverse Geocoding) 的功能
    /// </summary>
    public class GoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleMapsService> _logger;
        private const string GEOCODE_API_URL = "https://maps.googleapis.com/maps/api/geocode/json";

        public GoogleMapsService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 將地址轉換為地理座標 (Geocoding)
        /// </summary>
        /// <param name="request">地理編碼請求</param>
        /// <returns>包含座標資訊的回應</returns>
        public async Task<GeocodeResponse> GeocodeAddressAsync(GeocodeRequest request)
        {
            try
            {
                var apiKey = _configuration["GoogleMaps:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Google Maps API Key is not configured");
                    return new GeocodeResponse
                    {
                        Success = false,
                        ErrorMessage = "Google Maps API Key is not configured"
                    };
                }

                var queryParams = new Dictionary<string, string>
                {
                    { "address", request.Address },
                    { "key", apiKey }
                };

                if (!string.IsNullOrEmpty(request.Language))
                {
                    queryParams.Add("language", request.Language);
                }

                if (!string.IsNullOrEmpty(request.Region))
                {
                    queryParams.Add("region", request.Region);
                }

                var url = BuildUrl(GEOCODE_API_URL, queryParams);
                _logger.LogInformation("Geocoding address: {Address}", request.Address);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<GoogleMapsApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return ProcessApiResponse(apiResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while geocoding address");
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while geocoding address");
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 將地理座標轉換為地址 (Reverse Geocoding)
        /// </summary>
        /// <param name="request">反向地理編碼請求</param>
        /// <returns>包含地址資訊的回應</returns>
        public async Task<GeocodeResponse> ReverseGeocodeAsync(ReverseGeocodeRequest request)
        {
            try
            {
                var apiKey = _configuration["GoogleMaps:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Google Maps API Key is not configured");
                    return new GeocodeResponse
                    {
                        Success = false,
                        ErrorMessage = "Google Maps API Key is not configured"
                    };
                }

                var latlng = $"{request.Latitude},{request.Longitude}";
                var queryParams = new Dictionary<string, string>
                {
                    { "latlng", latlng },
                    { "key", apiKey }
                };

                if (!string.IsNullOrEmpty(request.Language))
                {
                    queryParams.Add("language", request.Language);
                }

                var url = BuildUrl(GEOCODE_API_URL, queryParams);
                _logger.LogInformation("Reverse geocoding coordinates: {Latitude}, {Longitude}", 
                    request.Latitude, request.Longitude);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<GoogleMapsApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return ProcessApiResponse(apiResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while reverse geocoding");
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reverse geocoding");
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批次地理編碼 (將多個地址轉換為座標)
        /// </summary>
        /// <param name="addresses">地址列表</param>
        /// <param name="language">語言設定</param>
        /// <param name="region">區域設定</param>
        /// <returns>地理編碼結果列表</returns>
        public async Task<List<GeocodeResponse>> BatchGeocodeAsync(
            List<string> addresses, 
            string? language = "zh-TW", 
            string? region = "TW")
        {
            var tasks = addresses.Select(address => 
                GeocodeAddressAsync(new GeocodeRequest 
                { 
                    Address = address, 
                    Language = language, 
                    Region = region 
                })
            );

            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// 更新避難所的座標資訊
        /// </summary>
        /// <param name="shelter">避難所物件</param>
        /// <returns>是否成功更新</returns>
        public async Task<bool> UpdateShelterCoordinatesAsync(Shelter shelter)
        {
            if (string.IsNullOrEmpty(shelter.Address))
            {
                _logger.LogWarning("Cannot geocode shelter {Name}: address is empty", shelter.Name);
                return false;
            }

            var response = await GeocodeAddressAsync(new GeocodeRequest
            {
                Address = shelter.Address
            });

            if (response.Success && response.Result != null)
            {
                shelter.Latitude = (float)response.Result.Latitude;
                shelter.Longitude = (float)response.Result.Longitude;
                _logger.LogInformation("Updated coordinates for shelter {Name}: {Lat}, {Lng}", 
                    shelter.Name, shelter.Latitude, shelter.Longitude);
                return true;
            }

            _logger.LogWarning("Failed to geocode shelter {Name}: {Error}", 
                shelter.Name, response.ErrorMessage);
            return false;
        }

        private string BuildUrl(string baseUrl, Dictionary<string, string> queryParams)
        {
            var query = string.Join("&", queryParams.Select(kvp => 
                $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
            return $"{baseUrl}?{query}";
        }

        private GeocodeResponse ProcessApiResponse(GoogleMapsApiResponse? apiResponse)
        {
            if (apiResponse == null)
            {
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to parse API response"
                };
            }

            if (apiResponse.Status != "OK")
            {
                var errorMessage = apiResponse.Status switch
                {
                    "ZERO_RESULTS" => "No results found for the given query",
                    "OVER_QUERY_LIMIT" => "API quota exceeded",
                    "REQUEST_DENIED" => "Request denied - check API key",
                    "INVALID_REQUEST" => "Invalid request parameters",
                    "UNKNOWN_ERROR" => "Server error - please try again",
                    _ => $"API returned status: {apiResponse.Status}"
                };

                _logger.LogWarning("Geocoding failed with status: {Status}", apiResponse.Status);
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            if (apiResponse.Results.Count == 0)
            {
                return new GeocodeResponse
                {
                    Success = false,
                    ErrorMessage = "No results found"
                };
            }

            var firstResult = apiResponse.Results[0];
            return new GeocodeResponse
            {
                Success = true,
                Result = new GeocodeResult
                {
                    FormattedAddress = firstResult.Formatted_Address,
                    Latitude = firstResult.Geometry.Location.Lat,
                    Longitude = firstResult.Geometry.Location.Lng,
                    PlaceId = firstResult.Place_Id,
                    LocationType = firstResult.Geometry.Location_Type,
                    AddressComponents = firstResult.Address_Components.Select(ac => new AddressComponent
                    {
                        LongName = ac.Long_Name,
                        ShortName = ac.Short_Name,
                        Types = ac.Types
                    }).ToList()
                }
            };
        }
    }
}
