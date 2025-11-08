namespace Backend.Models
{
    /// <summary>
    /// 地理編碼請求模型
    /// </summary>
    public class GeocodeRequest
    {
        public required string Address { get; set; }
        public string? Language { get; set; } = "zh-TW";
        public string? Region { get; set; } = "TW";
    }

    /// <summary>
    /// 反向地理編碼請求模型
    /// </summary>
    public class ReverseGeocodeRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Language { get; set; } = "zh-TW";
    }

    /// <summary>
    /// 地理編碼回應模型
    /// </summary>
    public class GeocodeResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public GeocodeResult? Result { get; set; }
    }

    /// <summary>
    /// 地理編碼結果
    /// </summary>
    public class GeocodeResult
    {
        public required string FormattedAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? PlaceId { get; set; }
        public List<AddressComponent> AddressComponents { get; set; } = new();
        public string? LocationType { get; set; }
    }

    /// <summary>
    /// 地址組成部分
    /// </summary>
    public class AddressComponent
    {
        public required string LongName { get; set; }
        public required string ShortName { get; set; }
        public List<string> Types { get; set; } = new();
    }

    /// <summary>
    /// Google Maps API 回應 (內部使用)
    /// </summary>
    internal class GoogleMapsApiResponse
    {
        public List<GoogleMapsResult> Results { get; set; } = new();
        public required string Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    internal class GoogleMapsResult
    {
        public List<GoogleAddressComponent> Address_Components { get; set; } = new();
        public required string Formatted_Address { get; set; }
        public required GoogleGeometry Geometry { get; set; }
        public required string Place_Id { get; set; }
        public List<string> Types { get; set; } = new();
    }

    internal class GoogleAddressComponent
    {
        public required string Long_Name { get; set; }
        public required string Short_Name { get; set; }
        public List<string> Types { get; set; } = new();
    }

    internal class GoogleGeometry
    {
        public required GoogleLocation Location { get; set; }
        public required string Location_Type { get; set; }
    }

    internal class GoogleLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
