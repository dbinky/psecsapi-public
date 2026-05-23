namespace psecsapi.api.models.Fleet;

public class FleetDetailResponseModel
{
    public Guid EntityId { get; set; }
    public Guid OwnerCorpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? LastUpdateTimestamp { get; set; }
    public DateTime? CreateTimestamp { get; set; }
    public Guid? SectorId { get; set; }
    public List<Guid>? Ships { get; set; }
    public string? Status { get; set; }
    public QueueState? QueueStatus { get; set; }
    public DateTime? TransitETA { get; set; }
    public Guid? ActiveCombatId { get; set; }
    public DateTime? LastCombatTimestamp { get; set; }
    public Guid? AssignedCombatScriptId { get; set; }
    public Guid? DestinationSectorId { get; set; }
    public decimal FleetSpeed { get; set; }
}
