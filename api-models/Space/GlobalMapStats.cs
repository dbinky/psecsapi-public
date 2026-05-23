namespace psecsapi.api.models.Space;

public class GlobalMapStats
{
    public int TotalSectors { get; set; }
    public Dictionary<string, int> SectorsByType { get; set; } = new();
}
