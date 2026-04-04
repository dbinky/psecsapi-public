namespace psecsapi.api.models.User;

public class TrancheInfoModel
{
    public decimal Amount { get; set; }
    public DateTime InvestedAt { get; set; }
    public DateTime? LastPayoutAt { get; set; }
    public bool IsEligibleToUninvest { get; set; }
}
