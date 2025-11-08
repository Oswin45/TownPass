using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class NaturalDisasterResponse
    {
        [JsonPropertyName("result")]
        public NaturalDisasterResult Result { get; set; }
    }

    public class NaturalDisasterResult
    {
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("sort")]
        public string Sort { get; set; }

        [JsonPropertyName("results")]
        public List<NaturalDisasterShelter> Results { get; set; }
    }

    public class NaturalDisasterShelter
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_importdate")]
        public ImportDate ImportDate { get; set; }

        [JsonPropertyName("收容所編號")]
        public string ShelterNumber { get; set; }

        [JsonPropertyName("名稱")]
        public string Name { get; set; }

        [JsonPropertyName("縣市")]
        public string City { get; set; }

        [JsonPropertyName("郵遞區號")]
        public string PostalCode { get; set; }

        [JsonPropertyName("鄉鎮")]
        public string District { get; set; }

        [JsonPropertyName("村里")]
        public string Village { get; set; }

        [JsonPropertyName("門牌地址")]
        public string Address { get; set; }

        [JsonPropertyName("類型")]
        public string Type { get; set; }

        [JsonPropertyName("水災")]
        public string FloodDisaster { get; set; }

        [JsonPropertyName("震災")]
        public string EarthquakeDisaster { get; set; }

        [JsonPropertyName("土石流")]
        public string Landslide { get; set; }

        [JsonPropertyName("海嘯")]
        public string Tsunami { get; set; }

        [JsonPropertyName("救濟支站")]
        public string ReliefStation { get; set; }

        [JsonPropertyName("無障礙設施")]
        public string AccessibleFacilities { get; set; }

        [JsonPropertyName("室內")]
        public string Indoor { get; set; }

        [JsonPropertyName("室外")]
        public string Outdoor { get; set; }

        [JsonPropertyName("服務里別")]
        public string ServiceVillages { get; set; }

        [JsonPropertyName("容納人數")]
        public string Capacity { get; set; }

        [JsonPropertyName("收容所面積（平方公尺）")]
        public string Area { get; set; }

        [JsonPropertyName("聯絡人姓名")]
        public string ContactPersonName { get; set; }

        [JsonPropertyName("聯絡人連絡電話")]
        public string ContactPersonPhone { get; set; }

        [JsonPropertyName("管理人姓名")]
        public string ManagerName { get; set; }

        [JsonPropertyName("管理人連絡電話")]
        public string ManagerPhone { get; set; }

        [JsonPropertyName("備考")]
        public string Remarks { get; set; }
    }

    public class ImportDate
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("timezone_type")]
        public int TimezoneType { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }
    }
}
