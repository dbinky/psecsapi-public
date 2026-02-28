namespace psecsapi.api.models.Market
{
    public class MarketListingItemModel
    {
        public Guid SaleId { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid SellerCorpId { get; set; }
        public string SellerCorpName { get; set; } = string.Empty;
        public string AssetSummary { get; set; } = string.Empty;
        public long Price { get; set; }
        public long? StartingPrice { get; set; }
        public int BidCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string TimeRemaining { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>Whether the requesting corp can afford this listing at the current price.</summary>
        public bool CanAfford { get; set; }
        /// <summary>Credits the requesting corp still needs to afford this listing. Null when CanAfford is true.</summary>
        public long? InsufficientFundsAmount { get; set; }
    }
}
