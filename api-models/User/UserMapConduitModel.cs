namespace psecsapi.api.models.User;

public class UserMapConduitModel
{
    public Guid EntityId { get; set; }
    public Guid DestinationSectorId { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
}
