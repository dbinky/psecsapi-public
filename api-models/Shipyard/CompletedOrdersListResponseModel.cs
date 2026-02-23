namespace psecsapi.api.models.Shipyard;

public class CompletedOrdersListResponseModel
{
    public List<CompletedOrderResponseModel> Orders { get; set; } = new();
    public int Total { get; set; }
}
