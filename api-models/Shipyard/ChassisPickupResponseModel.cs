using psecsapi.api.models.Ship;

namespace psecsapi.api.models.Shipyard;

public class ChassisPickupResponseModel
{
    public bool Success { get; set; }
    public Guid ShipId { get; set; }
    public ShipDetailResponseModel? ShipDetail { get; set; }
    public string? ErrorMessage { get; set; }
}
