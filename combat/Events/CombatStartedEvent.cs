using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// First event in the replay stream. Contains all information needed to initialize
/// a replay client: grid size, terrain layout, ship loadouts, and the RNG seed.
/// </summary>
public class CombatStartedEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.CombatStarted;

    public double GridWidth { get; set; }
    public double GridHeight { get; set; }
    public int RandomSeed { get; set; }
    public List<TerrainObstacleRecord> Terrain { get; set; } = new();
    public List<ShipLoadoutRecord> ShipLoadouts { get; set; } = new();

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteDouble(output, 1, GridWidth);
        WriteDouble(output, 2, GridHeight);
        WriteInt32(output, 3, RandomSeed);

        foreach (var obstacle in Terrain)
        {
            var obstacleBytes = obstacle.Serialize();
            output.WriteTag(4, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(obstacleBytes));
        }

        foreach (var loadout in ShipLoadouts)
        {
            var loadoutBytes = loadout.Serialize();
            output.WriteTag(5, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(loadoutBytes));
        }

        output.Flush();
        return memoryStream.ToArray();
    }

    public static CombatStartedEvent Deserialize(byte[] data)
    {
        var result = new CombatStartedEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.GridWidth = input.ReadDouble(); break;
                case 2: result.GridHeight = input.ReadDouble(); break;
                case 3: result.RandomSeed = input.ReadInt32(); break;
                case 4:
                    var obstacleBytes = input.ReadBytes();
                    result.Terrain.Add(TerrainObstacleRecord.Deserialize(obstacleBytes.ToByteArray()));
                    break;
                case 5:
                    var loadoutBytes = input.ReadBytes();
                    result.ShipLoadouts.Add(ShipLoadoutRecord.Deserialize(loadoutBytes.ToByteArray()));
                    break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
