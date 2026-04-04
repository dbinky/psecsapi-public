namespace psecsapi.api.models.User;

public class InvestmentInfoResponseModel
{
    public decimal TotalInvested { get; set; }
    public decimal EligibleToUninvest { get; set; }
    public long EstimatedNextPayout { get; set; }
    public DateTime NextPayoutTime { get; set; }
    public List<TrancheInfoModel> Tranches { get; set; } = new();
    public decimal DailyCreditsPerToken { get; set; }
}
