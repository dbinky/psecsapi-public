namespace psecsapi.api.models.Space;

public class EnhancedMapStatsResponseModel
{
    public GlobalMapStats Global { get; set; } = new();
    public PersonalMapStats? Personal { get; set; }
}
