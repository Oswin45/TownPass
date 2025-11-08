using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// 防空避難所資料模型
    /// Air Raid Shelter Data Model
    /// </summary>
    public class AirRaidShelter
    {
        /// <summary>
        /// 類別 (Category) - e.g., 一般住宅
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 電腦編號 (Computer ID)
        /// </summary>
        public string? ComputerId { get; set; }

        /// <summary>
        /// 名稱 (Name) - e.g., 公寓, 私人住宅大樓
        /// </summary>
        [Required]
        public required string Name { get; set; }

        /// <summary>
        /// 村里別 (Village/Borough)
        /// </summary>
        public string? Village { get; set; }

        /// <summary>
        /// 地址 (Address)
        /// </summary>
        [Required]
        public required string Address { get; set; }

        /// <summary>
        /// 緯度 (Latitude)
        /// </summary>
        [Range(-90, 90)]
        public double Latitude { get; set; }

        /// <summary>
        /// 經度 (Longitude)
        /// </summary>
        [Range(-180, 180)]
        public double Longitude { get; set; }

        /// <summary>
        /// 地下樓層數 (Underground Floor Number) - e.g., B01, B02
        /// </summary>
        public string? UndergroundFloor { get; set; }

        /// <summary>
        /// 可容納人數 (Capacity)
        /// </summary>
        [Range(0, int.MaxValue)]
        public int Capacity { get; set; }

        /// <summary>
        /// 轄管分局 (Police Station/Precinct)
        /// </summary>
        public string? Precinct { get; set; }

        /// <summary>
        /// 備註 (Remarks/Notes)
        /// </summary>
        public string? Notes { get; set; }
    }

    #region KML XML Models

    /// <summary>
    /// KML 根元素
    /// Root KML element
    /// </summary>
    [XmlRoot("kml", Namespace = "http://www.opengis.net/kml/2.2")]
    public class KmlRoot
    {
        [XmlElement("Document")]
        public KmlDocument? Document { get; set; }
    }

    /// <summary>
    /// KML Document 元素
    /// </summary>
    public class KmlDocument
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("description")]
        public string? Description { get; set; }

        [XmlElement("Style")]
        public List<KmlStyle>? Styles { get; set; }

        [XmlElement("StyleMap")]
        public List<KmlStyleMap>? StyleMaps { get; set; }

        [XmlElement("Folder")]
        public List<KmlFolder>? Folders { get; set; }
    }

    /// <summary>
    /// KML Folder 元素
    /// </summary>
    public class KmlFolder
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("Placemark")]
        public List<KmlPlacemark>? Placemarks { get; set; }
    }

    /// <summary>
    /// KML Placemark 元素 - 代表一個防空避難所位置標記
    /// </summary>
    public class KmlPlacemark
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("description")]
        public string? Description { get; set; }

        [XmlElement("styleUrl")]
        public string? StyleUrl { get; set; }

        [XmlElement("ExtendedData")]
        public KmlExtendedData? ExtendedData { get; set; }

        [XmlElement("Point")]
        public KmlPoint? Point { get; set; }
    }

    /// <summary>
    /// KML ExtendedData 元素 - 包含防空避難所詳細資料
    /// </summary>
    public class KmlExtendedData
    {
        [XmlElement("Data")]
        public List<KmlData>? Data { get; set; }

        /// <summary>
        /// 轉換為 AirRaidShelter 模型
        /// </summary>
        public AirRaidShelter ToAirRaidShelter(string? placemarkName, KmlPoint? point)
        {
            var shelter = new AirRaidShelter
            {
                Name = placemarkName ?? "未知",
                Address = GetDataValue("地址") ?? "未知"
            };

            shelter.Category = GetDataValue("類別");
            shelter.ComputerId = GetDataValue("電腦編號");
            shelter.Village = GetDataValue("村里別");
            shelter.UndergroundFloor = GetDataValue("地下樓層數");
            shelter.Precinct = GetDataValue("轄管分局");
            shelter.Notes = GetDataValue("備註");

            // 解析可容納人數
            var capacityStr = GetDataValue("可容納人數");
            if (!string.IsNullOrEmpty(capacityStr) && double.TryParse(capacityStr, out var capacity))
            {
                shelter.Capacity = (int)capacity;
            }

            // 解析座標
            if (point?.Coordinates != null)
            {
                var coords = point.Coordinates.Trim().Split(',');
                if (coords.Length >= 2)
                {
                    if (double.TryParse(coords[1], out var lat))
                        shelter.Latitude = lat;
                    if (double.TryParse(coords[0], out var lon))
                        shelter.Longitude = lon;
                }
            }
            else
            {
                // 嘗試從緯經度欄位解析
                var coordStr = GetDataValue("緯經度");
                if (!string.IsNullOrEmpty(coordStr))
                {
                    var coords = coordStr.Split(',');
                    if (coords.Length >= 2)
                    {
                        if (double.TryParse(coords[0], out var lat))
                            shelter.Latitude = lat;
                        if (double.TryParse(coords[1], out var lon))
                            shelter.Longitude = lon;
                    }
                }
            }

            return shelter;
        }

        private string? GetDataValue(string name)
        {
            return Data?.FirstOrDefault(d => d.Name == name)?.Value;
        }
    }

    /// <summary>
    /// KML Data 元素 - 單一資料欄位
    /// </summary>
    public class KmlData
    {
        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlElement("value")]
        public string? Value { get; set; }
    }

    /// <summary>
    /// KML Point 元素 - 座標資料
    /// </summary>
    public class KmlPoint
    {
        [XmlElement("coordinates")]
        public string? Coordinates { get; set; }
    }

    /// <summary>
    /// KML Style 元素
    /// </summary>
    public class KmlStyle
    {
        [XmlAttribute("id")]
        public string? Id { get; set; }

        [XmlElement("IconStyle")]
        public KmlIconStyle? IconStyle { get; set; }

        [XmlElement("LabelStyle")]
        public KmlLabelStyle? LabelStyle { get; set; }
    }

    /// <summary>
    /// KML StyleMap 元素
    /// </summary>
    public class KmlStyleMap
    {
        [XmlAttribute("id")]
        public string? Id { get; set; }

        [XmlElement("Pair")]
        public List<KmlPair>? Pairs { get; set; }
    }

    /// <summary>
    /// KML Pair 元素
    /// </summary>
    public class KmlPair
    {
        [XmlElement("key")]
        public string? Key { get; set; }

        [XmlElement("styleUrl")]
        public string? StyleUrl { get; set; }
    }

    /// <summary>
    /// KML IconStyle 元素
    /// </summary>
    public class KmlIconStyle
    {
        [XmlElement("color")]
        public string? Color { get; set; }

        [XmlElement("scale")]
        public double Scale { get; set; }

        [XmlElement("Icon")]
        public KmlIcon? Icon { get; set; }
    }

    /// <summary>
    /// KML Icon 元素
    /// </summary>
    public class KmlIcon
    {
        [XmlElement("href")]
        public string? Href { get; set; }
    }

    /// <summary>
    /// KML LabelStyle 元素
    /// </summary>
    public class KmlLabelStyle
    {
        [XmlElement("scale")]
        public double Scale { get; set; }
    }

    #endregion
}
