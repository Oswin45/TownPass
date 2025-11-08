using System.ComponentModel.DataAnnotations;

public enum DisasterTypes
{
    None = 0,
    Flooding = 1 << 0,
    Earthquake = 1 << 1,
    Landslide = 1 << 2,
    Tsunami = 1 << 3,
    AirRaid = 1 << 4
}
public class Shelter
{
    public int Id { get; set; }  // 資料庫自動遞增
    public string? Type { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; } 
    public DisasterTypes SupportedDisasters { get; set; }
    public bool Accesibility { get; set; }
    public required string Address { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }

    [Phone]
    public string? Telephone { get; set; }

    public int SizeInSquareMeters { get; set; }
}