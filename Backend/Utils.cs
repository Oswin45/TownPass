using Backend.Models;

public static class Utils
{
    /// <summary>
    /// 將 AirRaidShelter 轉換為 Shelter
    /// </summary>
    public static Shelter ConvertToShelter(this AirRaidShelter source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new Shelter
        {
            Type = source.Category ?? "防空避難所",
            Name = source.Name,
            Capacity = source.Capacity,
            SupportedDisasters = DisasterTypes.AirRaid, // 防空避難所支援空襲災害
            Accesibility = false, // KML 資料中無無障礙設施資訊，預設為 false
            Address = source.Address,
            Latitude = (float)source.Latitude,
            Longitude = (float)source.Longitude,
            Telephone = null, // KML 資料中無電話資訊
            SizeInSquareMeters = 0 // KML 資料中無面積資訊
        };
    }

    /// <summary>
    /// 將 NaturalDisasterShelter 轉換為 Shelter
    /// </summary>
    public static Shelter ConvertToShelter(this NaturalDisasterShelter source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // 解析災害類型
        var supportedDisasters = DisasterTypes.None; // 預設支援空襲

        if (!string.IsNullOrEmpty(source.FloodDisaster) && (source.FloodDisaster == "Y" || source.FloodDisaster == "備用"))
            supportedDisasters |= DisasterTypes.Flooding;

        if (!string.IsNullOrEmpty(source.EarthquakeDisaster) && (source.EarthquakeDisaster == "Y" || source.EarthquakeDisaster == "備用"))
            supportedDisasters |= DisasterTypes.Earthquake;

        if (!string.IsNullOrEmpty(source.Landslide) && (source.Landslide == "Y" || source.Landslide == "備用"))
            supportedDisasters |= DisasterTypes.Landslide;

        if (!string.IsNullOrEmpty(source.Tsunami) && (source.Tsunami == "是" || source.Tsunami == "備用"))
            supportedDisasters |= DisasterTypes.Tsunami;

        // 解析容納人數
        int.TryParse(source.Capacity?.Trim(), out int capacity);

        // 解析面積
        int.TryParse(source.Area?.Trim(), out int area);

        // 解析無障礙設施
        bool hasAccessibility = !string.IsNullOrEmpty(source.AccessibleFacilities) && source.AccessibleFacilities == "是";

        return new Shelter
        {
            Type = source.Type,
            Name = source.Name ?? "未命名收容所",
            Capacity = capacity,
            SupportedDisasters = supportedDisasters,
            Accesibility = hasAccessibility,
            Address = source.Address ?? "",
            Latitude = 0, // 需要從地址進行地理編碼或從其他來源取得
            Longitude = 0, // 需要從地址進行地理編碼或從其他來源取得
            Telephone = source.ContactPersonPhone ?? source.ManagerPhone,
            SizeInSquareMeters = area
        };
    }

}