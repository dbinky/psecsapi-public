namespace psecsapi.api.models.Market
{
    public class SaleResultResponseModel
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? SaleId { get; set; }
        public string? NewState { get; set; }
        public long? FeesCharged { get; set; }
        public long? CreditsTransferred { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? PickupWindowEndsAt { get; set; }
    }
}
