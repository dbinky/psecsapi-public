using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Shipyard;

public class PlaceBuildOrderRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string CatalogId { get; set; } = "";

    [Required]
    public Guid BlueprintInstanceId { get; set; }

    [Required]
    [MaxLength(50)]
    public Dictionary<string, List<Guid>> SelectedInputs { get; set; } = new();
}
