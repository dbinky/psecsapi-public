namespace psecsapi.api.models.Market;

public class MyBidsItemModel
{
    public Guid SaleId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SellerCorpName { get; set; } = string.Empty;
    public string AssetSummary { get; set; } = string.Empty;
    public long CurrentHighBid { get; set; }
    public int BidCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string TimeRemaining { get; set; } = string.Empty;
    public long YourBidAmount { get; set; }
    public string BidStatus { get; set; } = string.Empty;
}
