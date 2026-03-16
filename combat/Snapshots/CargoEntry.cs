using Orleans;
using psecsapi.Grains.Interfaces.BoxedAsset.Models;

namespace psecsapi.Combat.Snapshots;

/// <summary>
/// A cargo entry for tracking what a ship is carrying, used for loot drop calculation.
/// </summary>
[GenerateSerializer]
public record class CargoEntry
{
    [Id(0)] public Guid AssetId { get; init; }

    [Id(1)] public AssetType Type { get; init; }
}
