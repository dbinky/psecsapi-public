namespace psecsapi.api.Models.Mint;

[Serializable]
public class MintRateResponseModel
{
    public int CurrentRate { get; init; }
    public decimal RecentBurnVolume { get; init; }
    public int BaseRate { get; init; }
    public int FloorRate { get; init; }
    public int WindowHours { get; init; }
}
