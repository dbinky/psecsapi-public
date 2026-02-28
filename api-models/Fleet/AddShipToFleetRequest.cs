using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Fleet
{
    public class AddShipToFleetRequest
    {
        [Required]
        public Guid ShipId { get; set; }
    }
}
