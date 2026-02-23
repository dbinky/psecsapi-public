namespace psecsapi.api.models.Market
{
    public class RepostSaleRequest
    {
        public long Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsAuction { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
