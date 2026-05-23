namespace psecsapi.api.models.Space;

public class PersonalMapStats
{
    public int TotalKnown { get; set; }
    public Dictionary<string, int> SectorsByType { get; set; } = new();
    public int Favorites { get; set; }
}
