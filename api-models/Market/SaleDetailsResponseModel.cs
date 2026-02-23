namespace psecsapi.api.models.Market
{
    public class SaleDetailsResponseModel
    {
        public Guid SaleId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public Guid SellerCorpId { get; set; }
        public string SellerCorpName { get; set; } = string.Empty;
        public Guid? BuyerCorpId { get; set; }
        public string? BuyerCorpName { get; set; }
        public Guid BoxedAssetId { get; set; }
        public string AssetSummary { get; set; } = string.Empty;
        public long Price { get; set; }
        public long? StartingPrice { get; set; }
        public int BidCount { get; set; }
        public long MinimumNextBid { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset PickupWindowEndsAt { get; set; }
        public string TimeRemaining { get; set; } = string.Empty;
        public long StorageFeesPaid { get; set; }
    }
}
