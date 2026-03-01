namespace psecsapi.api.Models.Mint;

[Serializable]
public class MintBurnResponseModel
{
    public long CreditsReceived { get; init; }
    public int RateApplied { get; init; }
    public decimal TokensBurned { get; init; }
    public decimal NewTokenBalance { get; init; }
    public long NewCorpCredits { get; init; }
}
